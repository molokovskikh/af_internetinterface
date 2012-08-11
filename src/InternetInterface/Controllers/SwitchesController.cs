using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	public enum ClientTypeAll
	{
		[Description("Физические лица")]
		Physical,
		[Description("Юридические лица")]
		Lawyer,
		[Description("Все")]
		All
	}

	public class OnLineFilter : IPaginable, ISortableContributor, SortableContributor
	{
		public string SearchText { get; set; }
		public uint Switch { get; set; }
		public uint Zone { get; set; }
		public ClientTypeAll ClientType { get; set; }
		public string SortBy { get; set; }
		public string Direction { get; set; }

		private int _lastRowsCount;

		public int RowsCount
		{
			get { return _lastRowsCount; }
		}

		public int PageSize { get { return 30; } }

		public int CurrentPage { get; set; }

		public Dictionary<string, object> GetParameters()
		{
			return new Dictionary<string, object> {
				{"filter.SearchText", SearchText},
				{"filter.Switch", Switch},
				{"filter.Zone", Zone},
				{"filter.ClientType", ClientType},
				{"filter.SortBy", SortBy},
				{"filter.Direction", Direction},
				{"CurrentPage", CurrentPage}
			};
		}

		public string GetUri()
		{
			return string.Join("&", GetParameters().Where(p => p.Key != "CurrentPage").Select(p => string.Format("{0}={1}", p.Key, p.Value)).ToArray());
		}

		private string GetOrderField()
		{
			if (SortBy == "StaticIp")
				return string.Format("{0} {1} ", "CE.Ip", Direction);
			if (SortBy == "LeaseIp")
				return string.Format("{0} {1} ", "L.Ip", Direction);
			if (SortBy == "LeaseDate")
				return string.Format("{0} {1} ", "L.LeaseBegin", Direction);
			if (SortBy == "Client")
				return string.Format("{0} {1} ", "CE.Client", Direction);
			if (SortBy == "Endpoint")
				return string.Format("{0} {1} ", "CE.Id", Direction);
			if (SortBy == "Name")
				return string.Format("{0} {1} ", "C.Name", Direction);
			if (SortBy == "Switch")
				return string.Format("{0} {1} ", "L.Switch", Direction);
			if (SortBy == "SwitchName")
				return string.Format("{0} {1} ", "NS.Name", Direction);
			if (SortBy == "Port")
				return string.Format("{0} {1} ", "L.Port", Direction);
			if (SortBy == "PackageId")
				return string.Format("{0} {1} ", "CE.PackageId", Direction);
			if (SortBy == "Speed")
				return string.Format("{0} {1} ", "PS.Speed", Direction);
			if (SortBy == "SwitchIp")
				return string.Format("{0} {1}", "NS.ip", Direction);
			return string.Format("{0} {1} ", "C.Name", "asc");
		}

		public IList<ClientConnectInfo> Find(ISession DbSession)
		{
			var whereList = new List<string>();

			if (Zone > 0)
				whereList.Add("NS.Zone = :Zone");

			if (Switch > 0)
				whereList.Add("NS.Id = :Switch");

			if (!string.IsNullOrEmpty(SearchText))
				whereList.Add("LOWER(c.Name) like :SearchText");

			if (ClientType == ClientTypeAll.Lawyer)
				whereList.Add("c.LawyerPerson is not null");

			if (ClientType == ClientTypeAll.Physical)
				whereList.Add("c.PhysicalClient is not null");

			var wherePart = string.Empty;
			if (whereList.Count > 0)
				wherePart = "Where " + whereList.Implode(" AND ");

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
ORDER BY {2} {3}", selectText, wherePart, GetOrderField(), limitPart);

			var query = DbSession.CreateSQLQuery(sqlStr);
			SetParams(query);
			var result = query.ToList<ClientConnectInfo>();

			var fromPosition = selectText.IndexOf("from");
			var newSql = selectText.Remove(0, fromPosition);
			newSql = "select count(*) " + newSql + " " + wherePart;
			var countQuery = DbSession.CreateSQLQuery(newSql);
			SetParams(countQuery);
			_lastRowsCount = Convert.ToInt32(countQuery.UniqueResult());
			return result;
		}

		public void SetParams(IQuery query)
		{
			if (Zone > 0)
				query.SetParameter("Zone", Zone);
			if (Switch > 0)
				query.SetParameter("Switch", Switch);
			if (!string.IsNullOrEmpty(SearchText))
				query.SetParameter("SearchText", "%" + SearchText.ToLower() + "%");
		}
	}

	[Helper(typeof(PaginatorHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class SwitchesController : BaseController
	{
		public void ShowSwitches()
		{
			var switches = DbSession.CreateSQLQuery(
				@"SELECT NS.id, NS.Mac, inet_ntoa(NS.IP) as Ip, NS.Name, NS.Zone, NS.PortCount, NS.Comment FROM internet.NetworkSwitches NS")
				.AddEntity(typeof(NetworkSwitches)).List<NetworkSwitches>();
			PropertyBag["Switches"] = switches;
		}

		public void Delete(uint id)
		{
			var commutator = DbSession.Load<NetworkSwitches>(id);
			if (DbSession.Query<ClientEndpoint>().Any(e => e.Switch == commutator)) {
				Error("Коммутатор не может быть удален т.к. с ним работают клиенты");
				RedirectToReferrer();
			}
			else {
				DbSession.Delete(commutator);
				Notify("Удалено");
				RedirectToAction("ShowSwitches");
			}
		}

		public void MakeSwitch(uint Switch)
		{
			PropertyBag["Switch"] = DbSession.Load<NetworkSwitches>(Switch);
		}

		public void MakeSwitch()
		{
			PropertyBag["Switch"] = new NetworkSwitches {
				Zone = new Zone()
			};
			PropertyBag["Editing"] = false;
		}

		public void RegisterSwitch([ARDataBind("Switch", AutoLoadBehavior.NewRootInstanceIfInvalidKey)] NetworkSwitches @switch)
		{
			if (IsValid(@switch)) {
				@switch.IP = NetworkSwitches.SetProgramIp(@switch.IP);
				DbSession.SaveOrUpdate(@switch);
				RedirectToUrl("~/Switches/ShowSwitches.rails");
			}
			else
			{
				PropertyBag["Switch"] = @switch;
				RenderView("MakeSwitch");
			}
		}

		public void EditSwitch([ARDataBind("Switch", AutoLoad = AutoLoadBehavior.Always)] NetworkSwitches @switch)
		{
			if (IsValid(@switch)) {
				@switch.IP = NetworkSwitches.SetProgramIp(@switch.IP);
				DbSession.SaveOrUpdate(@switch);
				RedirectToUrl("~/Switches/ShowSwitches.rails");
			}
			else
			{
				PropertyBag["Switch"] = @switch;
				RenderView("MakeSwitch");
			}
		}

		public void FreePortForSwitch(string ids)
		{
			var id = !string.IsNullOrEmpty(ids) ? UInt32.Parse(ids) : DbSession.Query<NetworkSwitches>().First().Id;
			var commutator = DbSession.Get<NetworkSwitches>(id);

			var diniedPorts = DbSession.Query<ClientEndpoint>()
				.Where(c => c.Switch.Id == id)
				.Select(c => new { c.Port, client = c.Client.Id})
				.ToList();
			PropertyBag["commutator"] = commutator;
			PropertyBag["port_client"] = diniedPorts.ToDictionary(d => d.Port, d => d.client);
			PropertyBag["diniedPorts"] = diniedPorts.Select(p => p.Port).ToList();
			CancelLayout();
		}

		public void OnLineClient([DataBind("filter")]OnLineFilter filter)
		{
			PropertyBag["OnLineClients"] = filter.Find(DbSession);
			PropertyBag["Zones"] = Zone.FindAllSort();
			var switches = NetworkSwitches.All(DbSession);
			switches.Add(new NetworkSwitches(){Name = "Все"});
			PropertyBag["Switches"] = switches;
			PropertyBag["filter"] = filter;
			PropertyBag["SortBy"] = filter.SortBy;
			PropertyBag["Direction"] = filter.Direction;
		}
	}
}