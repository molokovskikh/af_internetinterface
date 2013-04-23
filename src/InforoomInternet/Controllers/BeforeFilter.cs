﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InforoomInternet.Logic;
using InternetInterface.Models;

namespace InforoomInternet.Controllers
{
	public class Redirecter
	{
		public static void RedirectRoot(Controller controller)
		{
			controller.RedirectToUrl(controller.Context.ApplicationPath + "/");
		}
	}


	public class NHibernateFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			var holder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = holder.CreateSession(typeof(ActiveRecordBase));
			try {
				session.EnableFilter("HiddenTariffs");
			}
			finally {
				holder.ReleaseSession(session);
			}
			return true;
		}
	}

	public class EditAccessFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			return LoginLogic.IsAccessiblePartner(context.Session["LoginPartner"]);
		}
	}

	public class BeforeFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			controllerContext.PropertyBag["ViewName"] = Path.GetFileNameWithoutExtension(context.Request.Uri.Segments.Last());
			controllerContext.PropertyBag["LocalPath"] = Path.GetFileNameWithoutExtension(context.Request.Uri.LocalPath);
			controllerContext.PropertyBag["loadInternetModules"] = !Lease.IsGray(context.Request.UserHostAddress);

			controllerContext.PropertyBag["authorized"] = AccessFilter.Authorized(context);
			if (context.Session["LoginPartner"] == null) {
				context.Session["LoginPartner"] = context.CurrentUser.Identity.Name;
			}

			controllerContext.PropertyBag["AccessEditLink"] = LoginLogic.IsAccessiblePartner(context.Session["LoginPartner"]);
			return true;
		}
	}

	public class AccessFilter : IFilter
	{
		public static bool Authorized(IEngineContext context)
		{
			var ip = context.Request.UserHostAddress;

			Lease[] lease = null;

			if (Regex.IsMatch(ip, NetworkSwitch.IPRegExp))
				lease = Lease.FindAllByProperty("Ip", Convert.ToUInt32(NetworkSwitch.SetProgramIp(ip)));

			if (lease != null && lease.Length != 0) {
				var client = lease.Where(l => l.Endpoint != null
					&& l.Endpoint.Client != null
					&& l.Endpoint.Client.PhysicalClient != null)
					.Select(l => l.Endpoint.Client)
					.FirstOrDefault();
				if (client != null) {
					context.Session["LoginClient"] = client.Id;
					context.Session["autoIn"] = true;
					return true;
				}
			}
			if ((context.Session["LoginClient"] == null) || (Client.Find(Convert.ToUInt32(context.Session["Login"])) == null)) {
				return false;
			}
			return true;
		}

		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			var authorized = Authorized(context);
			if (!authorized)
				context.Response.RedirectToUrl(@"..//Login/LoginPage");
			return authorized;
		}
	}
}