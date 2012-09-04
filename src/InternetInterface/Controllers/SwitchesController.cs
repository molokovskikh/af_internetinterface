using System;
using System.ComponentModel;
using System.Linq;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using NHibernate.Linq;

namespace InternetInterface.Controllers
{
	public enum ClientTypeAll
	{
		[Description("Физические лица")] Physical,
		[Description("Юридические лица")] Lawyer,
		[Description("Все")] All
	}

	[Helper(typeof(PaginatorHelper))]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(AuthenticationFilter))]
	public class SwitchesController : BaseController
	{
		public void ShowSwitches()
		{
			var switches = DbSession.CreateSQLQuery(
				@"SELECT NS.id, NS.Mac, inet_ntoa(NS.IP) as Ip, NS.Name, NS.Zone, NS.PortCount, NS.Comment FROM internet.NetworkSwitches NS")
				.AddEntity(typeof(NetworkSwitch)).List<NetworkSwitch>();
			PropertyBag["Switches"] = switches;
		}

		public void Delete(uint id)
		{
			var commutator = DbSession.Load<NetworkSwitch>(id);
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
			PropertyBag["Switch"] = DbSession.Load<NetworkSwitch>(Switch);
		}

		public void MakeSwitch()
		{
			PropertyBag["Switch"] = new NetworkSwitch {
				Zone = new Zone()
			};
			PropertyBag["Editing"] = false;
		}

		public void RegisterSwitch([ARDataBind("Switch", AutoLoadBehavior.NewRootInstanceIfInvalidKey)] NetworkSwitch @switch)
		{
			if (IsValid(@switch)) {
				@switch.IP = NetworkSwitch.SetProgramIp(@switch.IP);
				DbSession.SaveOrUpdate(@switch);
				RedirectToUrl("~/Switches/ShowSwitches.rails");
			}
			else {
				PropertyBag["Switch"] = @switch;
				RenderView("MakeSwitch");
			}
		}

		public void EditSwitch([ARDataBind("Switch", AutoLoad = AutoLoadBehavior.Always)] NetworkSwitch @switch)
		{
			if (IsValid(@switch)) {
				@switch.IP = NetworkSwitch.SetProgramIp(@switch.IP);
				DbSession.SaveOrUpdate(@switch);
				RedirectToUrl("~/Switches/ShowSwitches.rails");
			}
			else {
				PropertyBag["Switch"] = @switch;
				RenderView("MakeSwitch");
			}
		}

		public void FreePortForSwitch(string ids)
		{
			var id = !string.IsNullOrEmpty(ids) ? UInt32.Parse(ids) : DbSession.Query<NetworkSwitch>().First().Id;
			var commutator = DbSession.Get<NetworkSwitch>(id);

			var diniedPorts = DbSession.Query<ClientEndpoint>()
				.Where(c => c.Switch.Id == id)
				.Select(c => new { c.Port, client = c.Client.Id })
				.ToList();
			PropertyBag["commutator"] = commutator;
			PropertyBag["port_client"] = diniedPorts.ToDictionary(d => d.Port, d => d.client);
			PropertyBag["diniedPorts"] = diniedPorts.Select(p => p.Port).ToList();
			CancelLayout();
		}

		public void OnLineClient([SmartBinder("filter")] OnLineFilter filter)
		{
			PropertyBag["filter"] = filter;
			PropertyBag["OnLineClients"] = filter.Find(DbSession);
			PropertyBag["staticIps"] = filter.FindStatic(DbSession);
		}
	}
}