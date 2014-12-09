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
		public ActionResult Login(string username, string password, string returnUrl, bool shouldRemember = false)
		{
			int id = 0;
			int.TryParse(username, out id);
			var user = DbSession.Query<Client>().FirstOrDefault(k => k.Id == id);
			if (user != null && PasswordHasher.Equals(password, user.PhysicalClient.Salt, user.PhysicalClient.Password)) {
				return Authenticate("Profile", "Personal", username, shouldRemember);
			}
			ErrorMessage("Неправильный логин или пароль");
			if (String.IsNullOrEmpty(returnUrl))
				returnUrl = Request.UrlReferrer.ToString();

			if(!String.IsNullOrEmpty(returnUrl))
				return Redirect(returnUrl);
			else
				return RedirectToAction("Index", "Home");
		}


		public ActionResult Logout()
		{
			FormsAuthentication.SignOut();
			return RedirectToAction("Index", "Home");
		}
		
	}
}