using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using Castle.Components.Binder;
using Castle.MonoRail.ActiveRecordSupport;
using Castle.MonoRail.Framework;
using Common.Tools;
using Common.Web.Ui.Controllers;
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using InternetInterface.Helpers;
using Newtonsoft.Json;
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
	public class SwitchesController : InternetInterfaceController
	{
		public SwitchesController()
		{
			SetARDataBinder(AutoLoadBehavior.NewRootInstanceIfInvalidKey);
		}

		public void ShowSwitches(uint? zoneId, string format)
		{
			var query = DbSession.Query<NetworkSwitch>();
			if (zoneId != null) {
				query = query.Where(s => s.Zone.Id == zoneId);
			}
			var switches = query.OrderBy(s => s.Name).ToList();
			if (format.Match("json"))
				RenderJson(switches.Select(s => new { id = s.Id, name = s.Name }));
			else
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
			PropertyBag["Switch"] = new NetworkSwitch();
			PropertyBag["Editing"] = false;
		}

		public void RegisterSwitch()
		{
			var @switch = BindObject<NetworkSwitch>("Switch");
			if (IsValid(@switch)) {
				DbSession.Save(@switch);
				RedirectToUrl("~/Switches/ShowSwitches.rails");
			}
			else {
				PropertyBag["Switch"] = @switch;
				RenderView("MakeSwitch");
			}
		}

		public void EditSwitch()
		{
			var @switch = BindObject<NetworkSwitch>("Switch");
			if (IsValid(@switch)) {
				DbSession.Save(@switch);
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