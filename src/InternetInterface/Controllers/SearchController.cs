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

		public string Address
		{
			get {
				if (client.IsPhysical())
					return client.GetAdress();
				else {
					return client.LawyerPerson.ActualAdress;
				}
			}
		}

		public virtual string ForSearchAddress(string query)
		{
			return TextHelper.SelectQuery(query, Address);
		}
	}

	public class SeachFilter : IPaginable, ISortableContributor, SortableContributor
	{
		public UserSearchProperties searchProperties { get; set; }
		public uint statusType { get; set; }
		public ClientTypeProperties clientTypeFilter { get; set; }
		public EnabledTypeProperties EnabledTypeProperties { get; set; }
		public string searchText { get; set; }
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
				{"filter.EnabledTypeProperties.Type", EnabledTypeProperties.Type},
				{"filter.statusType", statusType},
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
			if (statusType > 0)
				query.SetParameter("statusType", statusType);
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
				return string.Format("{0} {1} ", "if (c.PhysicalClient is null, l.Tariff, t.Price)", Direction);
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
			if (InitializeContent.Partner.IsDiller() && string.IsNullOrEmpty(searchText))
				return new List<ClientInfo>();

			var selectText = @"SELECT 
c.*,
co.Contact
FROM internet.Clients c
left join internet.PhysicalClients p on p.id = c.PhysicalClient
left join internet.LawyerPerson l on l.id = c.LawyerPerson
left join internet.Contacts co on co.Client = c.id
left join internet.Tariffs t on t.Id = p.Tariff
left join internet.houses h on h.Id = p.HouseObj
join internet.Status S on s.id = c.Status";

			IList<Client> result = new List<Client>();
			ArHelper.WithSession(session =>
			{
				if (searchProperties == null)
					searchProperties = new UserSearchProperties();
				searchProperties.SearchText = searchText;
				var sqlStr = string.Empty;
				IQuery query = null;
				var limitPart = InitializeContent.Partner.IsDiller() ? "Limit 5 " : string.Format("Limit {0}, {1}", CurrentPage * PageSize, PageSize);
				var wherePart = GetClientsLogic.GetWhere(this);

				sqlStr = String.Format(
@"{0}
{1}
group by c.id
ORDER BY {2} {3}", selectText, wherePart, GetOrderField(), limitPart);
				query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Client));
				SetParameters(query);
				if (searchText != null && wherePart.Contains(":SearchText"))
					query.SetParameter("SearchText", "%" + searchText.ToLower() + "%");

				result = query.List<Client>();

				if (!InitializeContent.Partner.IsDiller()) {
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
				}

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
			AddIndispensableParameters();
		}


		public void SearchUsers(string query, PhysicalClient sClients)
		{
			var filter = new SeachFilter {
				searchProperties = new UserSearchProperties {SearchBy = SearchUserBy.Auto},
				EnabledTypeProperties = new EnabledTypeProperties {Type = EndbledType.All},
				statusType = 0,
				clientTypeFilter = new ClientTypeProperties {Type = ForSearchClientType.AllClients},
			};
			PropertyBag["filter"] = filter;
			PropertyBag["SearchText"] = "";
			PropertyBag["ChTariff"] = 0;
			PropertyBag["ChRegistr"] = 0;
			PropertyBag["ChBrigad"] = 0;
			PropertyBag["Connected"] = false;
			PropertyBag["ChAdditional"] = 0;
			if (sClients != null)
			{
				Flash["SClients"] = sClients;
			}
			AddIndispensableParameters();
		}

		private void AddIndispensableParameters()
		{
			PropertyBag["Statuses"] = Status.FindAllAdd();
			PropertyBag["Brigads"] = Brigad.FindAllAdd();
			PropertyBag["additionalStatuses"] = AdditionalStatus.FindAllAdd();
			PropertyBag["Tariffs"] = Tariff.FindAllAdd();
			PropertyBag["WhoRegistered"] = Partner.FindAllAdd();
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
