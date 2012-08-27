using System;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Controllers;
using InternetInterface.Models;
using NHibernate;

namespace InternetInterface.Queries
{
	public class StaticIpSearchResult
	{
		public uint ClientId { get; set; }
		public string Name { get; set; }

		public uint EndpointId { get; set; }
		public uint? PackageId { get; set; }

		public uint Port { get; set; }

		public string Ip { get; set; }
		public int? Speed { get; set; }

		public uint SwitchId { get; set; }
		public string SwitchName { get; set; }
		public string SwitchIp { get; set; }
	}

	public class OnLineFilter : PaginableSortable
	{
		public OnLineFilter()
		{
			SortKeyMap = new Dictionary<string, string> {
				{ "Name", "C.Name" },
				{ "StaticIp", "CE.Ip" },
				{ "LeaseIp", "L.Ip" },
				{ "LeaseDate", "L.LeaseBegin" },
				{ "Client", "CE.Client" },
				{ "Endpoint", "CE.Id" },
				{ "Switch", "L.Switch" },
				{ "SwitchName", "NS.Name" },
				{ "Port", "L.Port" },
				{ "PackageId", "CE.PackageId" },
				{ "Speed", "PS.Speed" },
				{ "SwitchIp", "NS.ip" },
			};
		}

		public string SearchText { get; set; }
		public NetworkSwitch Switch { get; set; }
		public Zone Zone { get; set; }
		public ClientTypeAll ClientType { get; set; }

		public IList<ClientConnectInfo> Find(ISession dbSession)
		{
			var wherePart = WherePart(false);

			var limitPart = string.Format("Limit {0}, {1}", CurrentPage * PageSize, PageSize);

			var selectText = @"
select
inet_ntoa(CE.Ip) as static_IP,
inet_ntoa(L.Ip) as Leased_IP,
CE.Client,
CE.Id as endpointId,
C.Name,
L.Switch,
NS.Name as Swith_adr,
inet_ntoa(NS.ip) as swith_IP,
L.Port,
CE.PackageId,
PS.Speed,
L.LeaseBegin
from internet.Leases L
left join internet.ClientEndpoints CE on L.Endpoint = CE.Id
left join internet.NetworkSwitches NS on NS.Id = L.Switch
left join internet.Clients C on CE.Client = C.Id
left join internet.PackageSpeed PS on PS.PackageId = CE.PackageId";

			var sqlStr = String.Format(
				@"{0}
{1}
group by l.id
ORDER BY {2} {3}
{4}", selectText, wherePart, GetSortProperty(), GetSortDirection(), limitPart);

			var query = dbSession.CreateSQLQuery(sqlStr);
			SetParams(query);
			var result = query.ToList<ClientConnectInfo>();

			var fromPosition = selectText.IndexOf("from");
			var newSql = selectText.Remove(0, fromPosition);
			newSql = "select count(*) " + newSql + " " + wherePart;
			var countQuery = dbSession.CreateSQLQuery(newSql);
			SetParams(countQuery);
			RowsCount = Convert.ToInt32(countQuery.UniqueResult());
			return result;
		}

		private string WherePart(bool searchInStatic)
		{
			var whereList = new List<string>();

			if (Zone != null)
				whereList.Add("NS.Zone = :Zone");

			if (Switch != null)
				whereList.Add("NS.Id = :Switch");

			if (!string.IsNullOrEmpty(SearchText)) {
				if (searchInStatic)
					whereList.Add("s.Ip like :SearchText");
				else
					whereList.Add("(c.Name like :SearchText or inet_ntoa(l.Ip) like :SearchText)");
			}

			if (ClientType == ClientTypeAll.Lawyer)
				whereList.Add("c.LawyerPerson is not null");

			if (ClientType == ClientTypeAll.Physical)
				whereList.Add("c.PhysicalClient is not null");

			var wherePart = string.Empty;
			if (whereList.Count > 0)
				wherePart = " Where " + whereList.Implode(" AND ");
			return wherePart;
		}

		public void SetParams(IQuery query)
		{
			if (Zone != null)
				query.SetParameter("Zone", Zone.Id);
			if (Switch != null)
				query.SetParameter("Switch", Switch.Id);
			if (!string.IsNullOrEmpty(SearchText))
				query.SetParameter("SearchText", "%" + SearchText.ToLower() + "%");
		}

		public IList<StaticIpSearchResult> FindStatic(ISession dbSession)
		{
			if (string.IsNullOrEmpty(SearchText))
				return Enumerable.Empty<StaticIpSearchResult>().ToList();

			var sql = @"
select
	ce.Client as ClientId,
	ce.Id as EndpointId,
	ce.PackageId,
	ce.Switch as SwitchId,
	ce.Port,
	c.Name,
	s.Ip,
	ps.Speed,
	ns.Name as SwitchName,
	inet_ntoa(ns.ip) as SwitchIp
from Internet.StaticIps s
	join internet.ClientEndpoints ce on s.Endpoint = ce.Id
	left join internet.PackageSpeed ps on ps.PackageId = ce.PackageId
	join internet.Clients c on ce.Client = c.Id
	join internet.NetworkSwitches ns on ns.Id = ce.Switch";

			var part = WherePart(true);
			sql += part;
			var query = dbSession.CreateSQLQuery(sql);
			SetParams(query);
			return query.ToList<StaticIpSearchResult>();
		}
	}
}