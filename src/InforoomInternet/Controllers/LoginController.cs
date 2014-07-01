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
		public void LoginPage(bool partner, uint? impersonate)
		{
			if (!partner) {
				PropertyBag["AcceptName"] = "AcceptClient";
			}
			else {
				if (LoginHelper.IsAccessiblePartner(Session["LoginPartner"])) {
					if (impersonate == null) {
						RedirectToSiteRoot();
					}
					else {
						Session["LoginClient"] = impersonate;
						Redirect("PrivateOffice", "IndexOffice");
					}
					return;
				}
				PropertyBag["AcceptName"] = "AcceptPartner";
				PropertyBag["impersonate"] = impersonate;
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void AcceptPartner(string login, string password, uint? impersonate)
		{
			if (ActiveDirectoryHelper.IsAuthenticated(login, password)) {
				Logger.Info("Авторизация выполнена");
				Session["LoginPartner"] = login;
				FormsAuthentication.SetAuthCookie(login, true);
				if (impersonate != null) {
					Session["LoginClient"] = impersonate;
					Redirect("PrivateOffice", "IndexOffice");
				}
				else {
					RedirectToSiteRoot();
				}
			}
			else {
				Logger.Info("Авторизация отклонена");
				Redirect("Login", "LoginPage", new { partner = true, impersonate });
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void AcceptClient(string login, string password)
		{
			try {
				var client = LoginHelper.IsAccessibleClient(Convert.ToUInt32(login), password);
				if (client != null) {
					Session["LoginClient"] = login;
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
			Session["LoginPartner"] = null;
			RedirectToSiteRoot();
		}
	}
}