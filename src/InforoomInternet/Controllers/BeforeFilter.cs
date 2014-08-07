using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Common.Web.Ui.ActiveRecordExtentions;
using InforoomInternet.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;

namespace InforoomInternet.Controllers
{
	public class EditAccessFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			return LoginHelper.IsAccessiblePartner(context.Session["LoginPartner"]);
		}
	}

	public class BeforeFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			controllerContext.PropertyBag["ViewName"] = Path.GetFileNameWithoutExtension(context.Request.Uri.Segments.Last());
			controllerContext.PropertyBag["LocalPath"] = Path.GetFileNameWithoutExtension(context.Request.Uri.LocalPath);

			controllerContext.PropertyBag["authorized"] = AccessFilter.Authorized(context);
			if (context.Session["LoginPartner"] == null) {
				context.Session["LoginPartner"] = context.CurrentUser.Identity.Name;
			}

			controllerContext.PropertyBag["AccessEditLink"] = LoginHelper.IsAccessiblePartner(context.Session["LoginPartner"]);
			return true;
		}
	}

	public class AccessFilter : IFilter
	{
		public static bool Authorized(IEngineContext context)
		{
			return ArHelper.WithSession(s => {
				var ip = context.Request.UserHostAddress;
				var address = IPAddress.Parse(ip);

				var leases = s.Query<Lease>().Where(l => l.Ip == address).ToList();

				if (leases.Count != 0) {
					var client = leases.Where(l => l.Endpoint != null
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
			});
		}

		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			var authorized = Authorized(context);
			if (!authorized)
				context.Response.RedirectToUrl("~/Login/LoginPage");
			return authorized;
		}
	}
}