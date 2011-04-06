using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InforoomInternet.Logic;
using InforoomInternet.Models;
using InternetInterface.Models;

namespace InforoomInternet.Controllers
{
	public class BeforeFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			controllerContext.PropertyBag["ViewName"] = Path.GetFileNameWithoutExtension(context.Request.Uri.Segments.Last());
			controllerContext.PropertyBag["LocalPath"] = Path.GetFileNameWithoutExtension(context.Request.Uri.LocalPath);
			if (context.Session["LoginPartner"] == null)
			{ context.Session["LoginPartner"] = context.CurrentUser.Identity.Name; }
			controllerContext.PropertyBag["AccessEditLink"] = LoginLogic.IsAccessiblePartner(context.Session["LoginPartner"]);
			//var blockMenu = string.Empty;
			//var menu = MenuField.FindAll();
			//controllerContext.PropertyBag["MenuListItems"] = MenuField.FindAll();
			//controllerContext.PropertyBag["blockMenu"] = blockMenu;)
			return true;
		}
	}

	public class AccessFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			var ip = context.Request.UserHostAddress;
			var lease = Lease.FindAllByProperty("Ip", Convert.ToUInt32(NetworkSwitches.SetProgramIp(ip)));
			if (lease.Length != 0)
			{
				context.Session["LoginClient"] =
					lease.Where(l => l.Endpoint != null && l.Endpoint.Client != null && l.Endpoint.Client.PhisicalClient != null).
						Select(l => l.Endpoint.Client.PhisicalClient.Id).First();
				return true;
			}
			if ((context.Session["LoginClient"] == null) || (PhisicalClients.Find(Convert.ToUInt32(context.Session["Login"])) == null))
			{
				context.Response.RedirectToUrl(@"..\\Login\LoginPage");
				return false;
			}
			return true;
		}
	}
}