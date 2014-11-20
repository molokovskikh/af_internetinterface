using System;
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
		[HttpPost]
		public ActionResult Login(string username, string password, string returnUrl)
		{
			if (IsAdmin(username, password)) {
				return Authenticate("Index", "Admin", username);
			}
			int id = 0;
			int.TryParse(username, out id);
			var user = DbSession.Query<PhysicalClient>().FirstOrDefault(k => k.Id == id);
			if (user != null && PasswordHasher.Equals(password, user.Salt, user.Password)) {
				return Authenticate("Profile", "Personal", username);
			}

			ErrorMessage("Неправильный логин или пароль");
			return Redirect(returnUrl);
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

		private ActionResult Authenticate(string action, string controller, string username)
		{
			FormsAuthentication.SetAuthCookie(username, false);
			return RedirectToAction(action, controller);
		}
	}
}