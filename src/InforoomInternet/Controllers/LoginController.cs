using System;
using System.Web.Security;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using InforoomInternet.Helpers;
using InternetInterface.Helpers;
using log4net;

namespace InforoomInternet.Controllers
{
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	public class LoginController : BaseController
	{
		public void LoginPage(bool partner)
		{
			if (!partner)
				PropertyBag["AcceptName"] = "AcceptClient";
			else {
				if (LoginHelper.IsAccessiblePartner(Session["LoginPartner"]))
					this.RedirectRoot();
				PropertyBag["AcceptName"] = "AcceptPartner";
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void AcceptPartner(string Login, string Password)
		{
			if (ActiveDirectoryHelper.IsAuthenticated(Login, Password)) {
				Logger.Info("Авторизация выполнена");
				FormsAuthentication.RedirectFromLoginPage(Login, true);
				Session["LoginPartner"] = Login;
				this.RedirectRoot();
			}
			else {
				Logger.Info("Авторизация отклонена");
				RedirectToUrl(@"..//Login/LoginPage?partner=true");
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void AcceptClient(string Login, string Password)
		{
			try {
				var client = LoginHelper.IsAccessibleClient(Convert.ToUInt32(Login), Password);
				if (client != null) {
					Session["LoginClient"] = Login;
					RedirectToUrl(@"..//PrivateOffice/IndexOffice");
				}
				else {
					RedirectToUrl(@"..//Login/LoginPage");
				}
			}
			catch (Exception ex) {
				Logger.Error("Ошибка авторизации", ex);
				RedirectToUrl(@"..//Login/LoginPage");
			}
		}

		public void Exit()
		{
			Session["LoginClient"] = null;
			RedirectToSiteRoot();
		}
	}
}