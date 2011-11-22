using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Helpers;
using InternetInterface.AllLogic;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using log4net;
using MonoRail.Debugger.Toolbar;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace InternetInterface.Controllers
{
	public class ClientInfo
	{
		public ClientInfo(Client _client)
		{
			client = _client;
		}

		public Client client;
		public bool OnLine;
	}

	public class SeachFilter : IPaginable, ISortableContributor, SortableContributor
	{
		public UserSearchProperties searchProperties { get; set; }
		public ConnectedTypeProperties connectedType { get; set; }
		public ClientTypeProperties clientTypeFilter { get; set; }
		public uint tariff { get; set; }
		public uint whoregister { get; set; }
		public uint brigad { get; set; }
		public string searchText { get; set; }
		public uint addtionalStatus { get; set; }
		public string SortBy { get; set; }
		public string Direction { get; set; }

		private int _lastRowsCount;

		public int RowsCount
		{
			get { return _lastRowsCount; }
		}

		public int PageSize { get { return 30; } }

		public int CurrentPage { get; set; }

		public string[] ToUrl()
		{
			return
				GetParameters().Where(p => p.Key != "CurrentPage").Select(p => string.Format("{0}={1}", p.Key, p.Value))
					.ToArray();
		}

		public Dictionary<string,object> GetParameters()
		{
			if (CategorieAccessSet.AccesPartner("SSI"))
			return new Dictionary<string, object> {
													  {"filter.searchText", searchText},
													  {"filter.searchProperties.SearchBy", searchProperties.SearchBy},
													  {"filter.clientTypeFilter.Type", clientTypeFilter.Type},
													  {"filter.tariff", tariff},
													  {"filter.whoregister", whoregister},
													  {"filter.brigad", brigad},
													  {"filter.addtionalStatus", addtionalStatus},
													  {"filter.connectedType.Type", connectedType.Type},
													  {"filter.SortBy", SortBy},
													  {"filter.Direction", Direction},
													  {"CurrentPage", CurrentPage}
												  };
			return new Dictionary<string, object> {
													  {"filter.searchText", searchText},
													  {"CurrentPage", CurrentPage}
												  };
		}

		public string ToUrlQuery()
		{
			return string.Join("&", ToUrl());
		}

		public string GetUri()
		{
			return ToUrlQuery();
		}

		private void SetParameters(IQuery query)
		{
			if (whoregister != 0)
				query.SetParameter("whoregister", whoregister);
			if (tariff != 0)
				query.SetParameter("tariff", tariff);
			if (brigad != 0)
				query.SetParameter("Brigad", brigad);
			if (addtionalStatus != 0)
				query.SetParameter("addtionalStatus", addtionalStatus);
			if (connectedType.IsConnected())
				query.SetParameter("Connected", true);
			if (connectedType.IsNoConnected())
				query.SetParameter("Connected", false);
		}

		private string GetOrderField()
		{        
			if (SortBy == "Name")
				return string.Format("{0} {1} ", "C.Name", Direction);
			if (SortBy == "Id")
				return string.Format("{0} {1} ", "C.Id", Direction);
			if (SortBy == "TelNum")
				return string.Format("{0} {1} ", "if (c.PhysicalClient is null, l.Telephone, p.PhoneNumber)", Direction);
			if (SortBy == "RegDate")
				return string.Format("{0} {1} ", "C.RegDate", Direction);
			if (SortBy == "Tariff")
				return string.Format("{0} {1} ", "if (c.PhysicalClient is null, l.Speed, p.Tariff)", Direction);
			if (SortBy == "Balance")
				return string.Format("{0} {1} ", "if (c.PhysicalClient is null, l.Balance, p.Balance)", Direction);
			if (SortBy == "Status")
				return string.Format("{0} {1} ", "C.Status", Direction);
			if (SortBy == "Adress")
				return string.Format("{0} {1}", "if (c.PhysicalClient is null, l.ActualAdress, CONCAT(p.Street, p.House, p.CaseHouse, p.Apartment))", Direction);
			return string.Format("{0} {1} ", "C.Name", "ASC");
		}

		public IList<ClientInfo> Find()
		{
			IList<Client> result = new List<Client>();
			ArHelper.WithSession(session =>
			{
				if (searchProperties == null)
					searchProperties = new UserSearchProperties();
				searchProperties.SearchText = searchText;
				var sqlStr = string.Empty;
				IQuery query = null;
				if (CategorieAccessSet.AccesPartner("SSI"))
					if (!searchProperties.IsSearchAccount())
					{
						sqlStr =
							String.Format(
								@"SELECT c.*, co.Contact FROM internet.Clients c
left join internet.PhysicalClients p on p.id = c.PhysicalClient
left join internet.LawyerPerson l on l.id = c.LawyerPerson
left join internet.Contacts co on co.Client = c.id
join internet.Status S on s.id = c.Status
{0} group by c.id ORDER BY {3} Limit {1}, {2}",
								GetClientsLogic.GetWhere(searchProperties, connectedType, clientTypeFilter, whoregister, tariff, searchText, brigad,
										 addtionalStatus), CurrentPage * PageSize, PageSize, GetOrderField());
						query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Client));
						SetParameters(query);
						if (searchText != null)
							query.SetParameter("SearchText", "%" + searchText.ToLower() + "%");
					}
					else
					{
						sqlStr = string.Format(@"SELECT c.*, co.Contact FROM internet.Clients c
left join internet.PhysicalClients p on p.id = c.PhysicalClient
left join internet.LawyerPerson l on l.id = c.LawyerPerson
join internet.Status S on s.id = c.Status
left join internet.Contacts co on co.Client = c.id
where C.id = :SearchText group by c.id ORDER BY {2} Limit {0}, {1}", CurrentPage * PageSize, PageSize, GetOrderField());
						query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Client));
						if (searchText != null)
							query.SetParameter("SearchText", searchText.ToLower());
					}
				else
				{
					if (!string.IsNullOrEmpty(searchText))
						sqlStr = string.Format(@"SELECT c.*, co.Contact FROM internet.Clients c
left join internet.PhysicalClients p on p.id = c.PhysicalClient
left join internet.LawyerPerson l on l.id = c.LawyerPerson
join internet.Status S on s.id = c.Status
left join internet.Contacts co on co.Client = c.id
WHERE LOWER(C.Name) like {0} or LOWER(C.Id) like {0} or LOWER(co.Contact) like {0}
group by c.id
ORDER BY {3} Limit {1}, {2}", ":SearchText", CurrentPage * PageSize, PageSize, GetOrderField());
					else
					{
						return new List<Client>();
					}
					query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Client));
					if (!string.IsNullOrEmpty(searchText))
						query.SetParameter("SearchText", "%" + searchText.ToLower() + "%");
				}
				result = query.List<Client>();

				var newSql = sqlStr.Replace("c.*", "count(*)");
				newSql = newSql.Replace(", co.Contact", string.Empty);
				var limitPosition = newSql.IndexOf("Limit");
				newSql = newSql.Remove(limitPosition);
				newSql = string.Format("select count(*) from ({0}) as t1;", newSql);
				var countQuery = session.CreateSQLQuery(newSql);
				if (!string.IsNullOrEmpty(searchText))
					countQuery.SetParameter("SearchText", "%" + searchText.ToLower() + "%");
				if (CategorieAccessSet.AccesPartner("SSI"))
					if (!searchProperties.IsSearchAccount())
						SetParameters(countQuery);
				_lastRowsCount = Convert.ToInt32(countQuery.UniqueResult());

				return result;
			});

			var clientsInfo = result.Select(c => new ClientInfo(c)).ToList();
			var onLineClients =
				Lease.Queryable.Where(l => l.Endpoint.Client != null).Select(c => c.Endpoint.Client.Id).ToList();
			foreach (var clientInfo in clientsInfo.Where(clientInfo => onLineClients.Contains(clientInfo.client.Id)))
			{
				clientInfo.OnLine = true;
			}
			return clientsInfo.ToList();
		}
	}

	[Helper(typeof(PaginatorHelper)),
	Helper(typeof(CategorieAccessSet)),]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class SearchController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Get)]
		public void SearchBy([DataBind("filter")]SeachFilter filter)
		{
			PropertyBag["SClients"] = filter.Find();
			PropertyBag["Direction"] = filter.Direction;
			PropertyBag["SortBy"] = filter.SortBy;
			PropertyBag["filter"] = filter;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["WhoRegistered"] = Partner.FindAllSort();
			PropertyBag["ChTariff"] = filter.tariff;
			PropertyBag["ChRegistr"] = filter.whoregister;
			PropertyBag["ChBrigad"] = filter.brigad;
			PropertyBag["additionalStatuses"] = AdditionalStatus.FindAll();
			PropertyBag["ChAdditional"] = filter.addtionalStatus;
			PropertyBag["Brigads"] = Brigad.FindAllSort();
		}


		public void SearchUsers(string query, PhysicalClients sClients)
		{
			var filter = new SeachFilter {
											 searchProperties = new UserSearchProperties {SearchBy = SearchUserBy.Auto},
											 connectedType = new ConnectedTypeProperties {Type = ConnectedType.AllConnected},
											 clientTypeFilter =
												 new ClientTypeProperties {Type = ForSearchClientType.AllClients},
										 };
			PropertyBag["filter"] = filter;
			PropertyBag["Tariffs"] = Tariff.FindAllSort();
			PropertyBag["WhoRegistered"] = Partner.FindAllSort();
			PropertyBag["SearchText"] = "";
			PropertyBag["ChTariff"] = 0;
			PropertyBag["ChRegistr"] = 0;
			PropertyBag["ChBrigad"] = 0;
			PropertyBag["Brigads"] = Brigad.FindAllSort();
			PropertyBag["Connected"] = false;
			PropertyBag["additionalStatuses"] = AdditionalStatus.FindAll();
			PropertyBag["ChAdditional"] = 0;
			if (sClients != null)
			{
				Flash["SClients"] = sClients;
			}
		}

		public void Redirect([DataBind("filter")]ClientFilter filter)
		{
			var builder = string.Empty;
			foreach (string name in Request.QueryString)
				builder += String.Format("{0}={1}&", name, Request.QueryString[name]);
			builder = builder.Substring(0, builder.Length - 1);
			if (Client.Find(filter.ClientCode).GetClientType() == ClientType.Phisical)
			{
				RedirectToUrl(string.Format("../UserInfo/SearchUserInfo.rails?{0}" , builder));
			}
			else
			{
				RedirectToUrl(string.Format("../UserInfo/LawyerPersonInfo.rails?{0}", builder));
			}
			CancelView();
		}
	}
}
