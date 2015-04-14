using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Models;
using InternetInterface.Helpers;
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
			if (Request.IsAuthenticated)
				return RedirectToAction("Index", "Admin");
			return View();
		}

		[HttpPost]
		public ActionResult Login(string username, string password, string returnUrl, bool shouldRemember = false, string impersonateClient = "")
		{
			var employee = DbSession.Query<Employee>().FirstOrDefault(p => p.Login == username && !p.IsDisabled);
			if (ActiveDirectoryHelper.IsAuthenticated(username, password) && employee != null) {
				Session.Add("employee", employee.Id); 
		//	 	SetCookie("publick_ukey", employee.Id);
				return Authenticate("Index", "Admin", username, shouldRemember, impersonateClient);
			}
			ErrorMessage("Неправильный логин или пароль");
			return Redirect(returnUrl);
		}

		public ActionResult AdminLogout()
		{
			FormsAuthentication.SignOut();
			return RedirectToAction("Index");
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		public ActionResult ApplyImpersonation([EntityBinder] Client client)
		{
			return Authenticate("Index", "AdminAccount", Environment.UserName, false, client.Id.ToString());
		}
	}
}