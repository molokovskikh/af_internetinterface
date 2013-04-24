﻿using System;
using System.Web;
using System.Web.Security;
using Castle.MonoRail.Framework;
using InternetInterface.Helpers;
using InternetInterface.Models;


namespace InternetInterface.Controllers
{
	public class LoginController : SmartDispatcherController
	{
		/// <summary>
		/// Метод выполняется по нажатии. на кнопку "Войти"
		/// </summary>
		/// <param name="login"></param>
		/// <param name="password"></param>
		[AccessibleThrough(Verb.Post)]
		public void Sub(string login, string password)
		{
			if (ActiveDirectoryHelper.IsAuthenticated(login, password)) {
				FormsAuthentication.RedirectFromLoginPage(login, true);
				Session.Add("Login", login);
				RedirectToUrl(@"~/Map/SiteMap.rails");
			}
			else {
				Flash["AccessDenied"] = ActiveDirectoryHelper.ErrorMessage;
				RedirectToUrl(@"LoginPartner.rails");
			}
		}

		public void LoginPartner()
		{
			LayoutName = "NoMap";
			if (Context.Session["Login"] == null)
				Context.Session["Login"] = Context.CurrentUser.Identity.Name;
			if (Context.Session["Login"] != null && !String.IsNullOrEmpty(Context.Session["Login"].ToString()))
				RedirectToUrl(@"~/Map/SiteMap.rails");
		}
	}
}