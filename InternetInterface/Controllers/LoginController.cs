using Castle.MonoRail.Framework;
using InternetInterface.Models;

namespace InternetInterface.Controllers
{
	public class LoginController : SmartDispatcherController
	{
		[AccessibleThrough(Verb.Post)]
		public void Sub(string Login, string Password)
		{
			string BasePass = string.Empty;
			var BaseUser = Partner.FindAllByProperty("Login", Login);
			if ((BaseUser.Length != 0) && (Password != null))
			{
				 BasePass = BaseUser[0].Pass;
				 if (CryptoPass.GetHashString(Password) == BasePass)
				 {
					 Session.Add("HashPass", BasePass);
					 RedirectToUrl(@"..//Map/SiteMap.rails");
				 }
			}
			else
			{
				Flash["AccessDenied"] = true;
				RedirectToUrl(@"LoginPartner.rails?TextAccess=NO");
			}
		}

		[AccessibleThrough(Verb.Get)]
		public void LoginPartner(string TextAccess)
		{
			/*if (TextAccess != "NO")
			PropertyBag["TextAccess"] = "";*/
		}

	}
}