using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Castle.MonoRail.Framework;
using InforoomInternet.Models;

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
			var ip = string.Empty;
#if DEBUG
			ip = "91.219.4.4";
#else
			ip = context.Request.UserHostAddress;
#endif
			if (Lease.FindAllByProperty("Ip", Convert.ToUInt32(Lease.SetProgramIp(ip))).Length != 0)
				return true;
			if ((context.Session["Login"] == null) || (PhysicalClient.Find(context.Session["Login"]) == null))
			{
				context.Response.RedirectToUrl(@"..\\Login\LoginClient.brail");
				return false;
			}
			else
			{
				return true;
			}
		}
	}
}