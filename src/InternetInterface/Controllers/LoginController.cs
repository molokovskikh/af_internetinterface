using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Security;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;


namespace InternetInterface.Controllers
{
	[Layout("NoMap")]
	public class LoginController : BaseController
	{
		/// <summary>
		/// Метод выполняется по нажатии. на кнопку "Войти"
		/// </summary>
		[AccessibleThrough(Verb.Post)]
		public void Sub(string login, string password)
		{
			if (ActiveDirectoryHelper.IsAuthenticated(login, password)
			    && DbSession.Query<Partner>().Any(p => p.Login == login && !p.IsDisabled)) {
				FormsAuthentication.RedirectFromLoginPage(login, true);
				AuthenticationFilter.SetLoginCookie(Context, login);
				RedirectToUrl(@"~/Map/SiteMap");
			}
			else {
				Error(ActiveDirectoryHelper.ErrorMessage ?? "Пользователь заблокирован");
				RedirectToAction("LoginPartner");
			}
		}

		public void LoginPartner(string redirect)
		{
			string username = AuthenticationFilter.GetLoginFromCookie(Context);
#if DEBUG
			//username = username ?? Environment.UserName;
#endif
			if (username != null) {
				if (DbSession.Query<Partner>().Any(p => p.Login == username && !p.IsDisabled)) {
					AuthenticationFilter.SetLoginCookie(Context, username);
					RedirectToUrl(redirect ?? @"~/Map/SiteMap");
				}
			}
		}

#if DEBUG
		public void ChangeLoggedInPartner(string login, string redirect)
		{
			if (AuthenticationFilter.GetLoginFromCookie(Context) != null) {
				Response.RemoveCookie(FormsAuthentication.FormsCookieName);
			}
			AuthenticationFilter.SetLoginCookie(Context, login);
			RedirectToUrl(redirect ?? @"~/Map/SiteMap");
		}
#endif
	}
}