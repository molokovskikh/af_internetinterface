using System;
using System.Web.Security;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using InforoomInternet.Logic;
using InternetInterface.Helpers;
using log4net;

namespace InforoomInternet.Controllers
{
	[Layout("Main")]
	[FilterAttribute(ExecuteWhen.BeforeAction, typeof(BeforeFilter))]
	[Filter(ExecuteWhen.BeforeAction, typeof(NHibernateFilter))]
	public class LoginController : BaseController
	{
		private static readonly ILog _log = LogManager.GetLogger(typeof(LoginController));

		public void LoginPage(bool partner)
		{
			if (!partner)
				PropertyBag["AcceptName"] = "AcceptClient";
			else {
				if (LoginLogic.IsAccessiblePartner(Session["LoginPartner"]))
					Redirecter.RedirectRoot(Context, this);
				PropertyBag["AcceptName"] = "AcceptPartner";
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void AcceptPartner(string Login, string Password)
		{
			if (ActiveDirectoryHelper.IsAuthenticated(Login, Password)) {
				_log.Info("Авторизация выполнена");
				FormsAuthentication.RedirectFromLoginPage(Login, true);
				Session["LoginPartner"] = Login;
				Redirecter.RedirectRoot(Context, this);
			}
			else {
				_log.Info("Авторизация отклонена");
				RedirectToUrl(@"..//Login/LoginPage?partner=true");
			}
		}

		[AccessibleThrough(Verb.Post)]
		public void AcceptClient(string Login, string Password)
		{
			try {
				var client = LoginLogic.IsAccessibleClient(Convert.ToUInt32(Login), Password);
				if (client != null) {
					Session["LoginClient"] = Login;
					if (client.NoEndPoint()) {
						var userHostAddress = Request.UserHostAddress;
#if DEBUG
						userHostAddress = "192.168.0.1";
#endif
						DbSession.SaveOrUpdate(client.CreateAutoEndPont(userHostAddress));
					}
					RedirectToUrl(@"..//PrivateOffice/IndexOffice");
				}
				else {
					RedirectToUrl(@"..//Login/LoginPage");
				}
			}
			catch (Exception ex) {
				_log.Error("Ошибка авторизации", ex);
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