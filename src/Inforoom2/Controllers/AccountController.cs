using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using Inforoom2.Models;
using InternetInterface.Helpers;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Страница управления аутентификацией
	/// </summary>
	public class AccountController : Inforoom2Controller
	{
		[HttpPost]
		public ActionResult Login(string username, string password)
		{
			int id = 0;
			int.TryParse(username, out id);
			var user = DbSession.Query<Client>().FirstOrDefault(k => k.Id == id && k.PhysicalClient != null);
			if (user != null && CryptoPass.GetHashString(password) == user.PhysicalClient.Password) {
				return Authenticate("Profile", "Personal", username, true);
			}
			ErrorMessage("Неправильный логин или пароль");
			if (Request.UrlReferrer != null) {
				var returnUrl = Request.UrlReferrer.ToString();
				return Redirect(returnUrl);
			}
			else {
				return RedirectToAction("Index", "Home");
			}
		}

		public ActionResult Login()
		{
			return View();
		}

		public ActionResult AdminLogin(string username = "", int clientId = 0)
		{
			ViewBag.ClientId = clientId.ToString();
			ViewBag.Username = username;
			return View();
		}

		[HttpPost]
		public ActionResult AdminLogin(string password, string username = "", int clientId = 0)
		{
			var user = DbSession.Query<Client>().FirstOrDefault(k => k.Id == clientId);
			var employee = DbSession.Query<Employee>().FirstOrDefault(p => p.Login == username && !p.IsDisabled);
			if (ActiveDirectoryHelper.IsAuthenticated(username, password) && employee != null && user != null) {
				Session.Add("employee", employee.Id);
				return Authenticate("Profile", "Personal", Environment.UserName, false, user.Id.ToString());
			}
			ErrorMessage("Неправильный логин или пароль");
			ViewBag.ClientId = clientId.ToString();
			ViewBag.Username = username;
			return View();
		}

		public ActionResult Logout()
		{
			FormsAuthentication.SignOut();
			return RedirectToAction("Index", "Home");
		}
	}
}