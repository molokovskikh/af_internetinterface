using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using Common.MySql;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Страница управления аутентификацией
	/// </summary>
	public class AccountController : BaseController
	{
		public ActionResult Login()
		{
			ViewBag.Username = "";
			ViewBag.Password = "";
			return View();
		}

		[HttpPost]
		public ActionResult Login(string username, string password, string returnUrl)
		{
			if (ModelState.IsValid) {
				if (IsAdmin(username, password)) {
					return Authenticate(Url.Content("~/Admin"), username);
				}

				var user = DbSession.Query<Client>().FirstOrDefault(k => k.Username == username);
				if (user != null && PasswordHasher.Equals(password, user.Salt, user.Password)) {
					return Authenticate(returnUrl, username);
				}
				ModelState.AddModelError("", "Неправильный логин или пароль");
			}
			// If we got this far, something failed, redisplay form
			return View();
		}


		public ActionResult Logout()
		{
			FormsAuthentication.SignOut();
			return RedirectToAction("Index", "Home");
		}

		private bool IsAdmin(string username, string password)
		{
			var admin = DbSession.Query<Employee>().FirstOrDefault(k => k.Username == username);
			if (admin != null && PasswordHasher.Equals(password, admin.Salt, admin.Password)) {
				return true;
			}
			return false;
		}

		private ActionResult Authenticate(string returnUrl, string username)
		{
			FormsAuthentication.SetAuthCookie(username, false);
			if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
			    && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\")) {
				return Redirect(returnUrl);
			}
			return RedirectToAction("Index", "Home");
		}
	}
}