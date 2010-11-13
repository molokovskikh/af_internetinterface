using Castle.MonoRail.Framework;
using InternetInterface.Models;

namespace InternetInterface.Controllers.Filter
{
	public class AuthenticationFilter : IFilter
	{
		public bool Perform(ExecuteWhen exec, IEngineContext context, IController controller, IControllerContext controllerContext)
		{
			var MapPartner = Partner.FindAllByProperty("Login", context.Session["Login"]);
			if (MapPartner.Length == 0)
			{
				context.Response.RedirectToUrl(@"..\\Errors\AccessDin.aspx");
				return false;
			}
			else
			{
				InithializeContent.partner = Partner.GetPartnerForLogin(context.Session["login"].ToString());
				return true;
			}
			
		}
	}
}