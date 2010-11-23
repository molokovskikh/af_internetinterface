using System.Linq;
using Castle.MonoRail.Framework;
using InternetInterface.Models;
using InternetInterface.Models.Access;

namespace InternetInterface.Controllers.Filter
{
	public class AuthenticationFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
#if DEBUG
			context.Session["Login"] = "zolotarev";
#endif
			if (Partner.FindAllByProperty("Login", context.Session["Login"]).Length == 0)
			{
				context.Response.RedirectToUrl(@"..\\Login\LoginPartner.brail");
				return false;
			}
			else
			{
				InithializeContent.partner = Partner.GetPartnerForLogin(context.Session["login"].ToString());
				controllerContext.PropertyBag["PartnerAccessSet"] = new PartnerAccessSet();
				controllerContext.PropertyBag["PARTNERNAME"] = InithializeContent.partner.Name;
				if (AccessRules.GetAccessName(controllerContext.Action).Count(PartnerAccessSet.AccesPartner) == 0)
				{
					context.Response.RedirectToUrl(@"..\\Errors\AccessDin.aspx");
					return false;
				}
				return true;
			}
			
		}
	}
}