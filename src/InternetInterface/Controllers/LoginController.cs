﻿using Castle.MonoRail.Framework;
using InternetInterface.Helpers;


namespace InternetInterface.Controllers
{
	public class LoginController : SmartDispatcherController
	{
		/// <summary>
		/// Метод выполняется по нажати. на кнопку "Войти"
		/// </summary>
		/// <param name="Login"></param>
		/// <param name="Password"></param>
		[AccessibleThrough(Verb.Post)]
		public void Sub(string Login, string Password)
		{
			if (ActiveDirectoryHelper.IsAuthenticated(Login, Password))
			{
				Session.Add("Login", Login);
				RedirectToUrl(@"..//Map/SiteMap.rails");
			}
			else
			{
				Flash["AccessDenied"] = ActiveDirectoryHelper.ErrorMessage;
				RedirectToUrl(@"LoginPartner.rails");
			}
		}

		public void LoginPartner()
		{
#if DEBUG
			RedirectToUrl(@"..//Map/SiteMap.rails");
#endif
		}
	}
}