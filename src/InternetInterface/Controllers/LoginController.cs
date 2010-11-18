using System;
using Castle.MonoRail.Framework;
using InternetInterface.Helpers;
using InternetInterface.Models;
using System.DirectoryServices;


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
			/*var MapPartner = Partner.FindAllByProperty("Login", Session["Login"]);
			if (MapPartner.Length == 0)
			{
				string BasePass = string.Empty;
				var BaseUser = Partner.FindAllByProperty("Login", Login);
				if ((BaseUser.Length != 0) && (Password != null))
				{
					BasePass = BaseUser[0].Pass;
					if (CryptoPass.GetHashString(Password) == BasePass)
					{
						Session.Add("Login", Login);
						RedirectToUrl(@"..//Map/SiteMap.rails");
					}
					else
					{
						Flash["AccessDenied"] = true;
						RedirectToUrl(@"LoginPartner.rails");
					}
				}
				else
				{
					Flash["AccessDenied"] = true;
					RedirectToUrl(@"LoginPartner.rails");
				}
			}
			else
			{
				RedirectToUrl(@"../Map/SiteMap.rails");
			}*/
		}


		public void LoginPartner()
		{
			/*if (TextAccess != "NO")
			PropertyBag["TextAccess"] = "";*/
		}

	}
}