using System;
using System.Web;
using System.Web.Security;
using Castle.MonoRail.Framework;
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
				FormsAuthentication.RedirectFromLoginPage(Login, true);
				Session.Add("Login", Login);
                //HttpContext.Current.Items.Add("Login", Login);
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
			if (Context.Session["Login"] == null)
			Context.Session["Login"] = Context.CurrentUser.Identity.Name;
			if (Context.Session["Login"] != null && !String.IsNullOrEmpty(Context.Session["Login"].ToString()))
				RedirectToUrl(@"..//Map/SiteMap.rails");
#if DEBUG
			//RedirectToUrl(@"..//Map/SiteMap.rails");
#endif
		}
	}
}