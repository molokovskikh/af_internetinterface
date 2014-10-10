using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	public class AccountController : BaseController
	{
		public ActionResult Login()
		{
			return View();
		}

		[HttpPost]
		public ActionResult Login(User model, string returnUrl)
		{
			// Lets first check if the Model is valid or not
			if (ModelState.IsValid) {
				string username = model.Username;
				string password = model.Password;

				// Now if our password was enctypted or hashed we would have done the
				// same operation on the user entered password here, But for now
				// since the password is in plain text lets just authenticate directly

				bool userValid = DBSession.Query<User>().Any(user => user.Username == username && user.Password == password);

				// User found in the database
				if (userValid) {

					FormsAuthentication.SetAuthCookie(username, false);
					if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
					    && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")) {
						return Redirect(returnUrl);
					}
					return RedirectToAction("Index", "Home");
				}
				ModelState.AddModelError("", "The user name or password provided is incorrect.");
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

	   public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }
	}
}