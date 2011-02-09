using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using Common.Models.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class SwitchesController : SmartDispatcherController
	{
		public void ShowSwitches()
		{
			IList<NetworkSwitches> switches = new List<NetworkSwitches>();
			ARSesssionHelper<NetworkSwitches>.QueryWithSession(session=>
			                                                   	{
			                                                   		var query =
			                                                   			session.CreateSQLQuery(
																			@"
SELECT NS.id, NS.Mac, inet_ntoa(NS.IP) as Ip, NS.Name, NS.Zone
FROM internet.NetworkSwitches NS").AddEntity(typeof(NetworkSwitches)).List<NetworkSwitches>();
			                                                   		switches = query;
			                                                   		return query;
			                                                   	});
			PropertyBag["Switches"] = switches;
		}

		public void MakeSwitch(uint Switch)
		{
			PropertyBag["Switch"] = NetworkSwitches.Find(Switch);
			PropertyBag["Zones"] = Zone.FindAllSort();
			PropertyBag["Editing"] = true;
			PropertyBag["VB"] = new ValidBuilderHelper<NetworkSwitches>(new NetworkSwitches());
		}

		public void MakeSwitch()
		{
			PropertyBag["Switch"] = new NetworkSwitches
			                        	{
			                        		Zone = new Zone()
			                        	};
			PropertyBag["Zones"] = Zone.FindAllSort();
			PropertyBag["Editing"] = false;
			PropertyBag["VB"] = new ValidBuilderHelper<NetworkSwitches>(new NetworkSwitches());
		}

		public void RegisterSwitch([DataBind("Switch")]NetworkSwitches Switch, uint Zoned, uint switchid)
		{
			if (Validator.IsValid(Switch))
			{
				Switch.Zone = Zone.Find(Zoned);
				Switch.IP = NetworkSwitches.SetProgramIp(Switch.IP);
				Switch.SaveAndFlush();
				RedirectToUrl("../Switches/ShowSwitches.rails");
			}
			else
			{
				RenderView("MakeSwitch");
				SendNoValidParam(Switch, switchid, Zoned);
				Flash["Editing"] = false;
			}
		}


		public void EditSwitch([DataBind("Switch")]NetworkSwitches Switch,uint Zoned, uint switchid)
		{
			if (Validator.IsValid(Switch))
			{
				var edSwitch = NetworkSwitches.Find(switchid);
				BindObjectInstance(edSwitch, ParamStore.Form, "Switch");
				edSwitch.Zone = Zone.Find(Zoned);
				edSwitch.IP = NetworkSwitches.SetProgramIp(Switch.IP);
				edSwitch.UpdateAndFlush();
				RedirectToUrl("../Switches/ShowSwitches.rails");
			}
			else
			{
				RenderView("MakeSwitch");
				SendNoValidParam(Switch, switchid, Zoned);
				Flash["Editing"] = true;
			}
		}

		private void SendNoValidParam(NetworkSwitches Switch, uint switchid, uint Zoned)
		{
			Switch.SetValidationErrors(Validator.GetErrorSummary(Switch));
			Switch.Zone = Zone.Find(Zoned);
			Switch.Id = switchid;
			Flash["VB"] = new ValidBuilderHelper<NetworkSwitches>(Switch);
			Flash["Switch"] = Switch;
			Flash["Zones"] = Zone.FindAllSort();
		}

		public void GoZone(int Zone)
		{
			RedirectToUrl(string.Format("../Switches/OnLineClient.rails?Zone={0}",Zone));
		}

		public void OnLineClient(int Zone)
		{
			IList<PhisicalClientConnectInfo> clients = new List<PhisicalClientConnectInfo>();
			ARSesssionHelper<object>.QueryWithSession(session =>
			{
				var query =
					session.CreateSQLQuery(string.Format(
						@"
select #*,
inet_ntoa(CE.Ip) as static_IP,
inet_ntoa(L.Ip) as Leased_IP,
CE.Client,
CE.Id as endpointId,
C.Name,
Ce.Switch,
NS.Name as Swith_adr,
inet_ntoa(NS.ip) as swith_IP,
CE.Module,
CE.Port,
CE.PackageId,
PS.Speed
from internet.Leases L
left join internet.ClientEndpoints CE on L.Endpoint = CE.Id
join internet.NetworkSwitches NS on NS.Id = CE.Switch
left join internet.Clients C on CE.Client = C.Id
left join internet.PackageSpeed PS on PS.PackageId = CE.PackageId
where NS.Zone = {0}", Zone)).SetResultTransformer(
									new AliasToPropertyTransformer(
										typeof(PhisicalClientConnectInfo)))
									.List<PhisicalClientConnectInfo>();
				clients = query;
				return query;
			});
			PropertyBag["OnLineClients"] = clients;
			PropertyBag["Zones"] = Models.Zone.FindAllSort();
			PropertyBag["thisZone"] = Zone;
		}
	}
}