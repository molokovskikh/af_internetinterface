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
				RedirectToAction("Impersonate");
			return View();
		}

		[HttpPost]
		public ActionResult Login(string username, string password, string returnUrl, bool shouldRemember = false, string impersonateClient = "")
		{
			if (ActiveDirectoryHelper.IsAuthenticated(username, password)
			    && DbSession.Query<Employee>().Any(p => p.Login == username && !p.IsDisabled)) {
					return Authenticate("Impersonate", "AdminAccount", username, shouldRemember, impersonateClient);
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
		public ActionResult Impersonate()
		{
			var clients = DbSession.Query<Client>().ToList();
			ViewBag.Clients = clients;
			return View();
		}

		[Authorize(Roles = "Admin")]
		[HttpPost]
		public ActionResult ApplyImpersonation([EntityBinder] Client client)
		{
			return Authenticate("Index", "Admin", Environment.UserName, false, client.Id.ToString());
		}
	}
}