using System;
using System.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
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

		public void GoZone(int Zone)
		{
			RedirectToUrl(string.Format("../Switches/OnLineClient.rails?Zone={0}",Zone));
		}

		public void FreePortForSwitch(string ids)
		{
			var id = !string.IsNullOrEmpty(ids) ? UInt32.Parse(ids) : DbSession.Query<NetworkSwitches>().First().Id;
			var commutator = DbSession.Get<NetworkSwitches>(id);
			var diniedPorts = ClientEndpoints.Queryable.Where(c => c.Switch.Id == id).ToList().Select(c => new { c.Port, client = c.Client.Id}).ToList();
			PropertyBag["commutator"] = commutator;
			PropertyBag["port_client"] = diniedPorts.ToDictionary(d => d.Port, d => d.client);
			PropertyBag["diniedPorts"] = diniedPorts.Select(p => p.Port).ToList();
			CancelLayout();
		}

		public void OnLineClient(int Zone)
		{
			var clients = DbSession.CreateSQLQuery(string.Format(@"
select #*,
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
left join internet.PackageSpeed PS on PS.PackageId = CE.PackageId
where NS.Zone = {0}", Zone))
				.ToList<ClientConnectInfo>();
			PropertyBag["OnLineClients"] = clients;
			PropertyBag["Zones"] = Models.Zone.FindAllSort();
			PropertyBag["thisZone"] = Zone;
		}
	}
}