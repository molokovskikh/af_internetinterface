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
			var MapPartner = Partner.FindAllByProperty("Login", context.Session["Login"]);
			if (MapPartner.Length == 0)
			{
				context.Response.RedirectToUrl(@"..\\Login\LoginPartner.brail");
				return false;
			}
			else
			{
				InithializeContent.partner = Partner.GetPartnerForLogin(context.Session["login"].ToString());
				var acclessList = AccessRules.GetAccessName(controllerContext.Action);
				var count = acclessList.Count(PartnerAccessSet.AccesPartner);
				/*var count = 0;
				foreach (var list in AcclessList)
				{
					if (PartnerAccessSet.AccesPartner(list))
					{
						count++;
					}
				}*/
				if (/*AcclessList.Count !=*/ count == 0)
				{
					context.Response.RedirectToUrl(@"..\\Errors\AccessDin.aspx");
					return false;
				}
				return true;
			}
			
		}
	}
}