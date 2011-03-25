using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InforoomInternet.Models;
using InternetInterface.Models;

namespace InforoomInternet.Controllers
{
	public class BeforeFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			controllerContext.PropertyBag["ViewName"] = Path.GetFileNameWithoutExtension(context.Request.Uri.Segments.Last());
			return true;
		}
	}

	public class AccessFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			/*if (context.Session["Login"] == null)
			{
				context.Session["Login"] = context.CurrentUser.Identity.Name;
			}*/
			var ip = context.Request.UserHostAddress;
			var lease = Lease.FindAllByProperty("Ip", Convert.ToUInt32(NetworkSwitches.SetProgramIp(ip)));
			if (lease.Length != 0)
			{
				context.Session["Login"] = lease.First().Endpoint.Client.PhisicalClient.Id;
				return true;
			}
			if ((context.Session["Login"] == null) || (PhisicalClients.Find(Convert.ToUInt32(context.Session["Login"])) == null))
			{
				context.Response.RedirectToUrl(@"..\\Login\LoginClient");
				return false;
			}
			else
			{
				return true;
			}
		}
	}
}