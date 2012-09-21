using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.AllLogic;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate;

namespace InternetInterface.Queries
{
	public class SeachFilter : IPaginable, ISortableContributor, IUrlContributor
	{
		public SeachFilter()
		{
			SearchProperties = new UserSearchProperties { SearchBy = SearchUserBy.Auto };
			ClientTypeFilter = new ClientTypeProperties { Type = ForSearchClientType.AllClients };
			EnabledTypeProperties = new EnabledTypeProperties { Type = EndbledType.All };
		}

		public UserSearchProperties SearchProperties { get; set; }
		public uint StatusType { get; set; }
		public ClientTypeProperties ClientTypeFilter { get; set; }
		public EnabledTypeProperties EnabledTypeProperties { get; set; }
		public string SearchText { get; set; }
		public string SortBy { get; set; }
		public string Direction { get; set; }
		public bool ExportInExcel { get; set; }

		private int _lastRowsCount;

		public int RowsCount
		{
			get { return _lastRowsCount; }
		}

		public int PageSize
		{
			get { return 30; }
		}

		public int CurrentPage { get; set; }

		public string[] ToUrl()
		{
			return
				GetParameters().Where(p => p.Key != "CurrentPage").Select(p => string.Format("{0}={1}", p.Key, p.Value))
					.ToArray();
		}

		public Dictionary<string, object> GetParameters()
		{
			if (CategorieAccessSet.AccesPartner("SSI"))
				return new Dictionary<string, object> {
					{ "filter.SearchText", SearchText },
					{ "filter.SearchProperties.SearchBy", SearchProperties.SearchBy },
					{ "filter.ClientTypeFilter.Type", ClientTypeFilter.Type },
					{ "filter.EnabledTypeProperties.Type", EnabledTypeProperties.Type },
					{ "filter.StatusType", StatusType },
					{ "filter.SortBy", SortBy },
					{ "filter.Direction", Direction },
					{ "CurrentPage", CurrentPage }
				};
			return new Dictionary<string, object> {
				{ "filter.searchText", SearchText },
				{ "CurrentPage", CurrentPage }
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
			if (StatusType > 0)
				query.SetParameter("statusType", StatusType);
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
			if (InitializeContent.Partner.IsDiller() && string.IsNullOrEmpty(SearchText))
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
			ArHelper.WithSession(session => {
				if (SearchProperties == null)
					SearchProperties = new UserSearchProperties();
				SearchProperties.SearchText = SearchText;
				var sqlStr = string.Empty;
				IQuery query = null;
				var limitPart = InitializeContent.Partner.IsDiller() ? "Limit 5 " : string.Format("Limit {0}, {1}", CurrentPage * PageSize, PageSize);
				if (ExportInExcel)
					limitPart = string.Empty;
				var wherePart = GetClientsLogic.GetWhere(this);

				sqlStr = String.Format(
					@"{0}
{1}
group by c.id
ORDER BY {2} {3}", selectText, wherePart, GetOrderField(), limitPart);
				query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Client));
				SetParameters(query);
				if (!string.IsNullOrEmpty(SearchText) && wherePart.Contains(":SearchText"))
					query.SetParameter("SearchText", "%" + SearchText.ToLower() + "%");

				result = query.List<Client>();

				if (!InitializeContent.Partner.IsDiller()) {
					var newSql = sqlStr.Replace("c.*", "count(*)");
					newSql = newSql.Replace(", co.Contact", string.Empty);
					if (!ExportInExcel) {
						var limitPosition = newSql.IndexOf("Limit");
						newSql = newSql.Remove(limitPosition);
					}
					newSql = string.Format("select count(*) from ({0}) as t1;", newSql);
					var countQuery = session.CreateSQLQuery(newSql);
					if (!string.IsNullOrEmpty(SearchText) && wherePart.Contains(":SearchText"))
						countQuery.SetParameter("SearchText", "%" + SearchText.ToLower() + "%");
					if (CategorieAccessSet.AccesPartner("SSI"))
						if (!SearchProperties.IsSearchAccount())
							SetParameters(countQuery);
					_lastRowsCount = Convert.ToInt32(countQuery.UniqueResult());
				}

				return result;
			});

			var clientsInfo = result.Select(c => new ClientInfo(c)).ToList();
			var onLineClients =
				Lease.Queryable.Where(l => l.Endpoint.Client != null).Select(c => c.Endpoint.Client.Id).ToList();
			foreach (var clientInfo in clientsInfo.Where(clientInfo => onLineClients.Contains(clientInfo.client.Id))) {
				clientInfo.OnLine = true;
			}
			return clientsInfo.ToList();
		}

		public IDictionary GetQueryString()
		{
			return PublicPropertiesToUrlParts("filter");
		}
		public Dictionary<string, object> PublicPropertiesToUrlParts(string filter)
		{
			var parts = new Dictionary<string, object>();
			foreach (var property in GetType().GetProperties()) {
				var methodInfo = property.GetSetMethod();
				if (methodInfo == null || !methodInfo.IsPublic)
					continue;

				var value = property.GetValue(this, null);
				if (value == null)
					continue;
				parts.Add(filter + "." + property.Name, value);
			}
			return parts;
		}
	}
}