using System;
using System.Web.Security;
using Castle.MonoRail.Framework;

namespace InternetInterface.Controllers
{
	public class ExitPartner : SmartDispatcherController
	{
		public void Yes()
		{
			Response.RemoveCookie(FormsAuthentication.FormsCookieName);
			RedirectToUrl(@"../Login/LoginPartner.rails");
		}
	}
}