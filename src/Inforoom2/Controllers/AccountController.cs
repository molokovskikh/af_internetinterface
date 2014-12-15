using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using Common.MySql;
using Inforoom2.Helpers;
using Inforoom2.Models;
using InternetInterface.Helpers;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Страница управления аутентификацией
	/// </summary>
	public class AccountController : BaseController
	{
		[HttpPost]
		public ActionResult Login(string username, string password)
		{
			int id = 0;
			int.TryParse(username, out id);
			var user = DbSession.Query<Client>().FirstOrDefault(k => k.Id == id);
			if (user != null && CryptoPass.GetHashString(password) == user.PhysicalClient.Password) {
				return Authenticate("Profile", "Personal", username, true);
			}
			ErrorMessage("Неправильный логин или пароль");
			var returnUrl = Request.UrlReferrer.ToString();
			return Redirect(returnUrl);
		}

		public ActionResult Login()
		{
			return View();
		}

		public ActionResult Logout()
		{
			FormsAuthentication.SignOut();
			return RedirectToAction("Index", "Home");
		}
		
	}
}