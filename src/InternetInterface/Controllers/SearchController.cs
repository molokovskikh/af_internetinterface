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
        public ClientInfo(Clients _client)
        {
            client = _client;
        }

        public Clients client;
        public bool OnLine;
    }

    public class SeachFilter : IPaginable
    {
        public UserSearchProperties searchProperties { get; set; }
        public ConnectedTypeProperties connectedType { get; set; }
        public ClientTypeProperties clientTypeFilter { get; set; }
        public uint tariff { get; set; }
        public uint whoregister { get; set; }
        public uint brigad { get; set; }
        public string searchText { get; set; }
        public uint addtionalStatus { get; set; }

        private int _lastRowsCount;

        public int RowsCount
        {
            get { return _lastRowsCount; }
        }

        public int PageSize { get { return 30; } }

        public int CurrentPage { get; set; }

        public string[] ToUrl()
        {
            if (CategorieAccessSet.AccesPartner("SSI"))
            return new[] {
				String.Format("filter.searchText={0}", searchText),
				String.Format("filter.searchProperties.SearchBy={0}", searchProperties.SearchBy),
				String.Format("filter.clientTypeFilter.Type={0}", clientTypeFilter.Type),
				String.Format("filter.tariff={0}", tariff),
				String.Format("filter.whoregister={0}", whoregister),
				String.Format("filter.brigad={0}", brigad),
				String.Format("filter.addtionalStatus={0}", addtionalStatus),
				String.Format("filter.connectedType.Type={0}", connectedType.Type),
			};
            return new[] {
                             String.Format("filter.searchText={0}", searchText)
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

        public IList<ClientInfo> Find()
        {
            IList<Clients> result = new List<Clients>();
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
                                @"SELECT * FROM internet.Clients c
left join internet.PhysicalClients p on p.id = c.PhysicalClient
left join internet.LawyerPerson l on l.id = c.LawyerPerson
join internet.Status S on s.id = c.Status
{0} ORDER BY C.Name Limit {1}, {2}",
                                GetClientsLogic.GetWhere(searchProperties, connectedType, clientTypeFilter, whoregister, tariff, searchText, brigad,
                                         addtionalStatus), CurrentPage * PageSize, PageSize);
                        query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Clients));
                        SetParameters(query);
                        if (searchText != null)
                            query.SetParameter("SearchText", "%" + searchText.ToLower() + "%");
                    }
                    else
                    {
                        sqlStr = string.Format(@"SELECT * FROM internet.Clients c
left join internet.PhysicalClients p on p.id = c.PhysicalClient
left join internet.LawyerPerson l on l.id = c.LawyerPerson
join internet.Status S on s.id = c.Status
where C.id = :SearchText ORDER BY C.name Limit {0}, {1}",  CurrentPage * PageSize, PageSize);
                        query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Clients));
                        //query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Clients));
                        if (searchText != null)
                            query.SetParameter("SearchText", searchText.ToLower());
                    }
                else
                {
                    if (!string.IsNullOrEmpty(searchText))
                        sqlStr = string.Format(@"SELECT * FROM internet.Clients c
left join internet.PhysicalClients p on p.id = c.PhysicalClient
left join internet.LawyerPerson l on l.id = c.LawyerPerson
join internet.Status S on s.id = c.Status
WHERE LOWER(C.Name) like {0} or LOWER(C.Id) like {0}
ORDER BY C.Name Limit {1}, {2}", ":SearchText", CurrentPage * PageSize, PageSize);
                    else
                    {
                        return new List<Clients>();
                        //sqlStr = @"SELECT * FROM internet.Clients C ORDER BY C.Name";
                    }
                    //query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Clients));
                    query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Clients));
                    if (!string.IsNullOrEmpty(searchText))
                        query.SetParameter("SearchText", "%" + searchText.ToLower() + "%");
                }
                //query.Add("SELECT FOUND_ROWS();");
                result = query.List<Clients>();

                var newSql = sqlStr.Replace("*", "count(*)");
                var limitPosition = newSql.IndexOf("Limit");
                var countQuery = session.CreateSQLQuery(newSql.Remove(limitPosition));
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

	[Layout("Main"),
    Helper(typeof(PaginatorHelper)),
    Helper(typeof(CategorieAccessSet))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class SearchController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Get)]
        public void SearchBy([DataBind("filter")]SeachFilter filter)
		{
            PropertyBag["SClients"] = filter.Find();
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
		                                     /*searchText = string.Empty,
		                                     addtionalStatus = 0,
		                                     brigad = 0,
		                                     tariff = 0,
		                                     whoregister = 0*/
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
            if (Clients.Find(filter.ClientCode).GetClientType() == ClientType.Phisical)
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
