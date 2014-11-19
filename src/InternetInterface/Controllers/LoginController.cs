﻿using System;
using System.Linq;
using System.Web;
using System.Web.Security;
using Castle.MonoRail.Framework;
using Common.Web.Ui.Controllers;
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
				Session.Add("Login", login);
				RedirectToUrl(@"~/Map/SiteMap");
			}
			else {
				Error(ActiveDirectoryHelper.ErrorMessage ?? "Пользователь заблокирован");
				RedirectToAction("LoginPartner");
			}
		}

		public void LoginPartner(string redirect)
		{
			var username = Context.Session["Login"];
#if DEBUG
			username = username ?? Environment.UserName;
#endif
			if (username != null) {
				if (DbSession.Query<Partner>().Any(p => p.Login == username && !p.IsDisabled)) {
					Session.Add("Login", username);
					RedirectToUrl(redirect ?? @"~/Map/SiteMap");
				}
			}
		}

#if DEBUG
		public void ChangeLoggedInPartner(string login, string redirect)
		{
			if (Session.Contains("Login")) {
				Session.Remove("Login");
			}
			Session.Add("Login", login);
			RedirectToUrl(redirect ?? @"~/Map/SiteMap");
		}
#endif
	}
}