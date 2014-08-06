using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Queries
{
	public class SearchFilter : Sortable, IPaginable
	{
		public SearchFilter()
		{
			SearchProperties = SearchUserBy.Auto;
			ClientTypeFilter = ForSearchClientType.AllClients;
			EnabledTypeProperties = EndbledType.All;
		}

		public SearchUserBy SearchProperties { get; set; }
		public uint StatusType { get; set; }
		public ForSearchClientType ClientTypeFilter { get; set; }
		public EndbledType EnabledTypeProperties { get; set; }
		public string SearchText { get; set; }
		public string Direction { get; set; }
		public bool ExportInExcel { get; set; }
		public RegionHouse Region { get; set; }

		public string City { get; set; }
		public string Street { get; set; }
		public string House { get; set; }
		public string CaseHouse { get; set; }
		public string Apartment { get; set; }

		public Service Service { get; set; }
		public RentableHardware RentableHardware { get; set; }

		public int? BlockDayCount { get; set; }

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
				GetParameters().Where(p => p.Key != "CurrentPage").Select(p => String.Format("{0}={1}", p.Key, p.Value))
					.ToArray();
		}

		public Dictionary<string, object> GetParameters()
		{
			if (CategorieAccessSet.AccesPartner("SSI"))
				return new Dictionary<string, object> {
					{ "filter.SearchText", SearchText },
					{ "filter.SearchProperties", SearchProperties },
					{ "filter.ClientTypeFilter", ClientTypeFilter },
					{ "filter.EnabledTypeProperties", EnabledTypeProperties },
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
			return String.Join("&", ToUrl());
		}

		public new string GetUri()
		{
			return ToUrlQuery();
		}

		private void SetParameters(IQuery query, string wherePart)
		{
			if (StatusType > 0)
				query.SetParameter("statusType", StatusType);

			if (Region != null)
				query.SetParameter("regionid", Region.Id);

			if (RentableHardware != null)
				query.SetParameter("hardwareId", RentableHardware.Id);

			if (BlockDayCount != null) {
				var blockBefore = DateTime.Today.AddDays(-BlockDayCount.Value);
				query.SetParameter("blockBefore", blockBefore);
			}
			if (Service != null)
				query.SetParameter("serviceId", Service.Id);

			if (SearchProperties == SearchUserBy.Address) {
				if (!String.IsNullOrEmpty(City))
					query.SetParameter("City", "%" + City + "%");
				if (!String.IsNullOrEmpty(Street))
					query.SetParameter("Street", "%" + Street + "%");
				if (!String.IsNullOrEmpty(House))
					query.SetParameter("House", House);
				if (!String.IsNullOrEmpty(CaseHouse))
					query.SetParameter("CaseHouse", "%" + CaseHouse + "%");
				if (!String.IsNullOrEmpty(Apartment))
					query.SetParameter("Apartment", Apartment);
			}
			else if (!String.IsNullOrEmpty(SearchText) && wherePart.Contains(":SearchText"))
				query.SetParameter("SearchText", "%" + SearchText + "%");
		}

		private string GetOrderField()
		{
			if (SortBy == "Name")
				return String.Format("{0} {1} ", "C.Name", Direction);
			if (SortBy == "Id")
				return String.Format("{0} {1} ", "C.Id", Direction);
			if (SortBy == "TelNum")
				return String.Format("{0} {1} ", "if (c.PhysicalClient is null, l.Telephone, p.PhoneNumber)", Direction);
			if (SortBy == "RegDate")
				return String.Format("{0} {1} ", "C.RegDate", Direction);
			if (SortBy == "Tariff")
				return String.Format("{0} {1} ", "if (c.PhysicalClient is null, l.Tariff, t.Price)", Direction);
			if (SortBy == "Balance")
				return String.Format("{0} {1} ", "if (c.PhysicalClient is null, l.Balance, p.Balance)", Direction);
			if (SortBy == "Status")
				return String.Format("{0} {1} ", "C.Status", Direction);
			if (SortBy == "Adress")
				return String.Format("{0} {1}", "if (c.PhysicalClient is null, l.ActualAdress, CONCAT(p.Street, p.House, p.CaseHouse, p.Apartment))", Direction);
			return String.Format("{0} {1} ", "C.Name", "ASC");
		}

		public IList<ClientInfo> Find(ISession session, bool forTest = false)
		{
			var partner = InitializeContent.Partner;
			if (!forTest && partner.IsDiller() && String.IsNullOrEmpty(SearchText))
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

			var limitPart = partner.IsDiller() ? "Limit 5 " : String.Format("Limit {0}, {1}", CurrentPage * PageSize, PageSize);
			if (ExportInExcel)
				limitPart = String.Empty;
			var wherePart = GetWhere();

			var sqlStr = String.Format(
				@"{0}
{1}
group by c.id
ORDER BY {2} {3}", selectText, wherePart, GetOrderField(), limitPart);
			var query = session.CreateSQLQuery(sqlStr).AddEntity(typeof(Client));
			SetParameters(query, wherePart);

			var result = query.List<Client>();

			if (!partner.IsDiller()) {
				var newSql = sqlStr.Replace("c.*", "count(*)");
				newSql = newSql.Replace(", co.Contact", String.Empty);
				if (!ExportInExcel) {
					var limitPosition = newSql.IndexOf("Limit");
					newSql = newSql.Remove(limitPosition);
				}
				newSql = String.Format("select count(*) from ({0}) as t1;", newSql);
				var countQuery = session.CreateSQLQuery(newSql);
				if (!String.IsNullOrEmpty(SearchText) && wherePart.Contains(":SearchText"))
					countQuery.SetParameter("SearchText", "%" + SearchText + "%");
				if (CategorieAccessSet.AccesPartner("SSI"))
					SetParameters(countQuery, wherePart);
				_lastRowsCount = Convert.ToInt32(countQuery.UniqueResult());
			}


			var clientsInfo = result.Select(c => new ClientInfo(c)).ToList();
			var onLineClients =
				session.Query<Lease>().Where(l => l.Endpoint.Client != null).Select(c => c.Endpoint.Client.Id).ToList();
			foreach (var clientInfo in clientsInfo.Where(clientInfo => onLineClients.Contains(clientInfo.client.Id))) {
				clientInfo.OnLine = true;
			}
			return clientsInfo.ToList();
		}

		public string GetWhere()
		{
			var result = String.Empty;
			if (!InitializeContent.Partner.IsDiller()) {
				if (StatusType > 0)
					result += " and S.Id = :statusType";

				if (ClientTypeFilter == ForSearchClientType.Physical)
					result += " and C.PhysicalClient is not null";

				if (ClientTypeFilter == ForSearchClientType.Lawyer)
					result += " and C.LawyerPerson is not null";

				if (EnabledTypeProperties == EndbledType.Disabled)
					result += " and c.Disabled";

				if (EnabledTypeProperties == EndbledType.Enabled)
					result += " and c.Disabled = false";

				if(Region != null) {
					result += " and (h.RegionId = :regionid or l.RegionId = :regionid)";
				}

				if (Service != null) {
					result += " and exists(select * from internet.ClientServices cs where cs.Client = c.Id and cs.Service = :serviceId) ";
				}

				if (RentableHardware != null) {
					result += " and exists(select * from internet.ClientServices cs where cs.Client = c.Id and cs.RentableHardware = :hardwareId) ";
				}

				if (BlockDayCount != null) {
					result += "and c.Disabled and c.BlockDate < :blockBefore";
				}

				if (SearchProperties != SearchUserBy.Address) {
					if (!String.IsNullOrEmpty(SearchText)) {
						if (SearchProperties == SearchUserBy.Auto) {
							return @"
	WHERE
	(C.Name like :SearchText or
	C.id like :SearchText or
	p.ExternalClientId like :SearchText or
	co.Contact like :SearchText or
	h.Street like :SearchText or
	l.ActualAdress like :SearchText)" + result;
						}
						if (SearchProperties == SearchUserBy.SearchAccount) {
							var id = 0u;
							UInt32.TryParse(SearchText, out id);
							if (id > 0)
								return String.Format("where C.id = {0}", id);
						}
						if (SearchProperties == SearchUserBy.OuterClientCode) {
							var id = 0u;
							UInt32.TryParse(SearchText, out id);
							if (id > 0)
								return String.Format("where p.ExternalClientId = {0}", id);
						}
						if (SearchProperties == SearchUserBy.ByFio) {
							return "WHERE (C.Name like :SearchText)" + result;
						}
						if (SearchProperties == SearchUserBy.TelNum) {
							return "WHERE (co.Contact like :SearchText)" + result;
						}
						if (SearchProperties == SearchUserBy.ByPassport) {
							return @"
	WHERE (p.PassportSeries like :SearchText or p.PassportNumber like :SearchText or l.ActualAdress like :SearchText)"
								+ result;
						}
					}
				}
				else {
					var where = "where";
					var whereCount = 0;
					if (!String.IsNullOrEmpty(City)) {
						@where += "(p.City like :City or l.ActualAdress like :City)";
						whereCount++;
					}
					if (!String.IsNullOrEmpty(Street)) {
						if (whereCount > 0)
							@where += " and ";
						@where += "(h.Street like :Street or l.ActualAdress like :Street or p.Street like :Street)";
						whereCount++;
					}
					if (!String.IsNullOrEmpty(House)) {
						if (whereCount > 0)
							@where += " and ";
						@where += "(p.House = :House)";
						whereCount++;
					}
					if (!String.IsNullOrEmpty(CaseHouse)) {
						if (whereCount > 0)
							@where += " and ";
						@where += "(p.CaseHouse like :CaseHouse or l.ActualAdress like :CaseHouse)";
						whereCount++;
					}
					if (!String.IsNullOrEmpty(Apartment)) {
						if (whereCount > 0)
							@where += " and ";
						@where += "(p.Apartment = :Apartment)";
						whereCount++;
					}

					if (whereCount == 0)
						@where += "(1 = 1)";

					return @where + result;
				}
			}
			else {
				var id = 0u;
				UInt32.TryParse(SearchText, out id);
				if (id > 0) {
					return String.Format("WHERE (c.Id = {0}) and (C.PhysicalClient is not null)", id);
				}
				if (!String.IsNullOrEmpty(SearchText))
					return "WHERE (C.Name like :SearchText) and (C.PhysicalClient is not null)";
			}
			return String.IsNullOrEmpty(result) ? String.Empty : String.Format("WHERE {0}", result.Remove(0, 4));
		}
	}
}