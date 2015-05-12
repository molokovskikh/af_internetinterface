using System;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;
using System.Security.Cryptography;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления аутентификацией
	/// </summary>
	public class AdminAccountController : BaseController
	{
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			ViewBag.BreadCrumb = "Главная";
		}

		public ActionResult Index()
		{
			if (User.Identity.IsAuthenticated)
				return RedirectToAction("Statistic", "Admin");
			return View();
		}

		[HttpPost]
		public ActionResult Login(string username, string password, string returnUrl, bool shouldRemember = false, string impersonateClient = "")
		{
			var employee = DbSession.Query<Employee>().FirstOrDefault(p => p.Login == username && !p.IsDisabled);
#if DEBUG
			//Авторизация для тестов, если пароль совпадает с паролем по умолчанию и логин есть в АД, то все ок
			var defaultPassword = ConfigurationManager.AppSettings["DefaultEmployeePassword"];
			if (employee != null && password == defaultPassword) {
				Session.Add("employee", employee.Id);
				return Authenticate("Statistic", "Admin", username, shouldRemember, impersonateClient);
			}
#endif
			if (ActiveDirectoryHelper.IsAuthenticated(username, password) && employee != null) { 
				Session.Add("employee", employee.Id);
				return Authenticate("Statistic", "Admin", username, shouldRemember, impersonateClient); 
			}
			ErrorMessage("Неправильный логин или пароль");
			return Redirect(returnUrl);
		}

		public ActionResult AdminLogout()
		{
			FormsAuthentication.SignOut();
			SetCookie(FormsAuthentication.FormsCookieName,null);
			return RedirectToAction("Index");
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		public ActionResult ApplyImpersonation([EntityBinder] Client client)
		{
			return Authenticate("Statistic", "AdminAccount", Environment.UserName, false, client.Id.ToString());
		}
	}
}