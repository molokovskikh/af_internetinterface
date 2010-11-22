using Castle.MonoRail.Framework;
using InternetInterface.Helpers;


namespace InternetInterface.Controllers
{
	public class LoginController : SmartDispatcherController
	{
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