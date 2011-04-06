using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using Castle.MonoRail.Framework;
using InforoomInternet.Logic;
using InforoomInternet.Models;
using InternetInterface.Helpers;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Dialect;
using NHibernate.Dialect.Function;
using NHibernate.Type;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	public class LoginController : SmartDispatcherController
	{
		public void LoginPage(bool partner)
		{
			if (!partner)
				PropertyBag["AcceptName"] = "AcceptClient";
			else
			{
				if (LoginLogic.IsAccessiblePartner(Session["LoginPartner"]))
					RedirectToSiteRoot();
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
				RedirectToSiteRoot();
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