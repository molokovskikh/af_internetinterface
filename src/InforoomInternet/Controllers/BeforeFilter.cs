using System;
using System.IO;
using System.Linq;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using InforoomInternet.Logic;
using InternetInterface.Models;

namespace InforoomInternet.Controllers
{
	public class Redirecter
	{
		public static void RedirectRoot(IEngineContext context, Controller controller)
		{
			controller.RedirectToUrl(context.ApplicationPath + "/");
		}
	}


	public class NHibernateFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			var holder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = holder.CreateSession(typeof(ActiveRecordBase));
			try
			{
				session.EnableFilter("HiddenTariffs");
			}
			finally
			{
				holder.ReleaseSession(session);
			}
			return true;
		}
	}


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
					lease.Where(l => l.Endpoint != null && l.Endpoint.Client != null && l.Endpoint.Client.PhysicalClient != null).
						Select(l => l.Endpoint.Client.Id).First();
				return true;
			}
			if ((context.Session["LoginClient"] == null) || (Clients.Find(Convert.ToUInt32(context.Session["Login"])) == null))
			{
				context.Response.RedirectToUrl(@"..//Login/LoginPage");
				return false;
			}
			return true;
		}
	}
}