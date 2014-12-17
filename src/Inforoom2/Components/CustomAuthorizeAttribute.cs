using System;
using System.Web.Mvc;

namespace Inforoom2.Components
{
	public class CustomAuthorizeAttribute : AuthorizeAttribute
	{
		public override void OnAuthorization(
			AuthorizationContext filterContext)
		{
			if (filterContext == null)
			{
				throw new ArgumentNullException("filterContext");
			}

			if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
			{
				string loginUrl = "/Account/Login"; // Default Login Url 
				filterContext.Result = new RedirectResult(loginUrl);
			}
		}
	}
}