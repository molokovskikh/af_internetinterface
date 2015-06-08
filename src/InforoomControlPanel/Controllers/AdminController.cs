using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Web.Mvc;
using Common.MySql;
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
		public ActionResult EmployeeList()
		{
			var employees = DbSession.Query<Employee>().OrderBy(i => i.Name).ToList();
			ViewBag.employees = employees;
			return View();
		}

		
		/// <summary>
		/// Редактирование администратора
		/// </summary>
		/// <returns></returns>
		public ActionResult EditEmployee(int id)
		{
			var employee = DbSession.Get<Employee>(id);
			var roles = DbSession.Query<Role>().ToList();
			roles = roles.Where(i => !employee.Roles.Any(j => j == i)).ToList();

			ViewBag.Employee = employee;
			ViewBag.Roles = roles;
			return View("EditEmployee");
		}

		/// <summary>
		///Редактирование администратора
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public ActionResult EditEmployee([EntityBinder] Employee employee)
		{
			var errors = ValidationRunner.ValidateDeep(employee);
			if (errors.Length == 0)
			{
				DbSession.Save(employee);
				SuccessMessage("Объект успешно изменен");
				RedirectToAction("EditRole");
			}
			EditEmployee(employee.Id);
			return View("EditEmployee");
		}	

		/// <summary>
		/// Список прав, которые можно назначать администраторам
		/// </summary>
		/// <returns></returns>
		public ActionResult PermissionList()
		{
			var rights = DbSession.Query<Permission>().ToList();
			ViewBag.Permissions = rights;
			return View();
		}
		/// <summary>
		/// Страница, отображаемая, в случае, если у пользователя нет прав
		/// </summary>
		/// <returns></returns>
		public ActionResult AccessDenined()
		{
			ErrorMessage("Вы попытались получить доступ к части системы для которой у вас нет прав!");
			return View();
		}

		public ActionResult RenewActionPermissions()
		{
			var controllers = GetType().Assembly.GetTypes().Where(i=> i.IsSubclassOf(typeof(ControlPanelController))).ToList();
			foreach (var controller in controllers) {
				var methods = controller.GetMethods();
				var actions = methods.Where(i => i.ReturnType == typeof(ActionResult)).ToList();
				foreach (var action in actions) {
					var name = controller.Name + "_" + action.Name;
					var right = DbSession.Query<Permission>().FirstOrDefault(i => i.Name == name);
					if(right != null)
						continue;
					var newright = new Permission();
					newright.Name = name;
					var url = Url.Action(action.Name,controller.Name.Replace("Controller",""));
					newright.Description = "Доступ к странице <a href='"+url+"'>"+name+"</a>";
					DbSession.Save(newright);
				}
			}
			return RedirectToAction("PermissionList");
		}

		/// <summary>
		/// Список ролей
		/// </summary>
		/// <returns></returns>
		public ActionResult RoleList()
		{
			var roles = DbSession.Query<Role>().ToList();
			ViewBag.Roles = roles;
			return View();
		}

		/// <summary>
		/// Редактирование роли
		/// </summary>
		/// <returns></returns>
		public ActionResult EditRole(int id)
		{
			var role = DbSession.Get<Role>(id);
			var permissions = DbSession.Query<Permission>().ToList();
			permissions = permissions.Where(i => !role.Permissions.Any(j => j == i)).ToList();

			ViewBag.Role = role;
			ViewBag.Permissions = permissions;
			return View("EditRole");
		}

		/// <summary>
		/// Редактирование роли
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public ActionResult EditRole([EntityBinder] Role role)
		{
			var errors = ValidationRunner.ValidateDeep(role);
			if (errors.Length == 0) {
				DbSession.Save(role);
				SuccessMessage("Объект успешно изменен");
				RedirectToAction("EditRole");
			}
			EditRole(role.Id);
			return View("EditRole");
		}

		/// <summary>
		/// Логи
		/// </summary> 
		public ActionResult LogRegResultList()
		{
			var pager = new ModelFilter<Log>(this, urlBasePrefix: "/", orderByColumn:"Id",orderDirrection: false);
			var logs = pager.GetCriteria().List<Log>();
			ViewBag.Pager = pager;
			ViewBag.Logs = logs;
			return View();
		}

		/// <summary>
		/// Логи
		/// </summary> 
		public ActionResult LogRegResultInfo(int Id = 0,string path="")
		{
			var log = DbSession.Query<Log>().FirstOrDefault(s => s.Id == Id); 
			ViewBag.Log = log;
			ViewBag.BackUrl = (path == string.Empty ? "/Admin/LogRegResultList" : path);
			return View();
		}
		
	}
}