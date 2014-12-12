using System;
using System.Web.Security;
using Castle.MonoRail.Framework;

namespace InternetInterface.Controllers
{
	public class ExitPartner : SmartDispatcherController
	{
		public void Yes()
		{
			FormsAuthentication.SignOut();
			RedirectToUrl(@"../Login/LoginPartner.rails");
		}
	}
}