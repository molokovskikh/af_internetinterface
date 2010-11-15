using System;
using Castle.MonoRail.Framework;

namespace InternetInterface.Controllers
{
	public class ExitPartner : SmartDispatcherController
	{
		public void Yes()
		{
			Session["Login"] = string.Empty;
			RedirectToUrl(@"../Login/LoginPartner.rails");
		}
	}
}