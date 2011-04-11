using System;
using System.Web.Security;
using Castle.MonoRail.Framework;
using InforoomInternet.Logic;
using InternetInterface.Helpers;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	public class LoginController : SmartDispatcherController
	{
		public void LoginPage(bool partner)
		{
			if (!partner)
				PropertyBag["AcceptName"] = "AcceptClient";
			else
			{
				if (LoginLogic.IsAccessiblePartner(Session["LoginPartner"]))
					Redirecter.RedirectRoot(Context, this);
					//RedirectToSiteRoot();
				PropertyBag["AcceptName"] = "AcceptPartner";
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void AcceptPartner(string Login, string Password)
		{
			if (ActiveDirectoryHelper.IsAuthenticated(Login, Password))
			{
				FormsAuthentication.RedirectFromLoginPage(Login, true);
				Session["LoginPartner"] = Login;
				//RedirectToSiteRoot();
				Redirecter.RedirectRoot(Context, this);
			}
			else
			{
				RedirectToUrl(@"..\\Login\LoginPage?partner=true");
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void AcceptClient(string Login, string Password)
		{
			try
			{
				var id = Convert.ToUInt32(Login);
				if (LoginLogic.IsAccessibleClient(id, Password))
				{
					Session["LoginClient"] = Login;
					RedirectToUrl(@"..\\PrivateOffice\IndexOffice");
				}
				else
				{
					RedirectToUrl(@"..\\Login\LoginPage");
				}
			}
			catch (Exception ex)
			{
				RedirectToUrl(@"..\\Login\LoginPage");
			}
		}
	}
}