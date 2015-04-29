using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Базовый контролер администратора
	/// </summary>
	[AuthorizeUser(Roles = "Admin")]
	public class AdminController : ControlPanelController
	{
		public AdminController()
		{
			ViewBag.BreadCrumb = "Панель управления";
		}


		public ActionResult Statistic()
		{
			return View("Index");
		}

		/// <summary>
		/// Список администраторов и создание нового
		/// </summary>
		/// <returns></returns>
		public ActionResult Admins()
		{
			var newAdmin = new Administrator();
			var admins = DbSession.Query<Administrator>().ToList();
			var employees = DbSession.Query<Employee>().OrderBy(i => i.Name).ToList().Where(e => admins.Where(a => a.Employee == e).ToList().Count == 0).ToList();
			ViewBag.employees = employees;
			ViewBag.admins = admins;
			ViewBag.Administrator = newAdmin;
			return View();
		}

		/// <summary>
		/// Создание нового администратора
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Admins([EntityBinder] Administrator Administrator)
		{
			var errors = ValidationRunner.ValidateDeep(Administrator);
			if (errors.Length == 0) {
				DbSession.Save(Administrator);
				SuccessMessage("Администратор успешно добавлен");
				return Admins();
			}
			Admins();
			ViewBag.newAdmin = Administrator;
			return View();
		}
	}
}