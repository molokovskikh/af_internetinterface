using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Web.Mvc;
using Common.MySql;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using Inforoom2.Models;
using InforoomControlPanel.Helpers;
using NHibernate.Linq;
using Remotion.Linq.Clauses;
using Decimal = NPOI.HPSF.Decimal;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Базовый контролер администратора
	/// </summary>
	public class AdminController : ControlPanelController
	{
		private List<Role> _roles;

		public AdminController()
		{
			ViewBag.BreadCrumb = "Панель управления";
		}


		public ActionResult Statistic()
		{
			var pager = new InforoomModelFilter<SiteVersionChange>(this);
			pager.SetOrderBy("Id", OrderingDirection.Desc);
			pager.GetCriteria();
			ViewBag.Pager = pager; 
			return View("Statistic");
		}

		[HttpGet]
		public ActionResult SaleSettings()
		{
			var stSettings = DbSession.Query<SaleSettings>().FirstOrDefault();
			if (stSettings == null) {
				throw new Exception("Настройки скидок не заданы (SaleSettings)");
			}
			ViewBag.SaleSettings = stSettings;
			return View();
		}

		[HttpPost]
		public ActionResult SaleSettings([EntityBinder] SaleSettings settings)
		{
			var errors = ValidationRunner.Validate(settings);
			if (errors.Count == 0) {
				DbSession.Save(settings);
				SuccessMessage("Обновление настроек сотрудников завершилось успешно");
			}
			else {
				DbSession.Refresh(settings);
				ErrorMessage("Возникла ошибка при обновлении настроек сотрудников");
				return View(SaleSettings());
			}
			return RedirectToAction("SaleSettings");
		}

		[HttpGet]
		public ActionResult EmployeeSettings()
		{
			var stSettings = DbSession.Query<EmployeeTariff>().ToList();
			if (stSettings[0].ActionName != "WorkedClient" || stSettings[1].ActionName != "AgentPayIndex") {
				throw new Exception("Настройки сотрудников не заданы (EmployeeTariff)");
			}
			ViewBag.EmployeeTariff = stSettings;
			return View();
		}

		[HttpPost]
		public ActionResult EmployeeSettings(string settingsA, string settingsB)
		{
			decimal valA, valB = 0;
			var stSettings = DbSession.Query<EmployeeTariff>().ToList();
			if (stSettings[0].ActionName != "WorkedClient" || stSettings[1].ActionName != "AgentPayIndex") {
				throw new Exception("Настройки сотрудников не заданы (EmployeeTariff)");
			}
			var resA = decimal.TryParse(settingsA.Replace(".", ","), out valA);
			var resB = decimal.TryParse(settingsB.Replace(".", ","), out valB);
			if (resA && resB) {
				stSettings[0].Sum = valA;
				stSettings[1].Sum = valB;
				DbSession.Save(stSettings[0]);
				DbSession.Save(stSettings[1]);
				SuccessMessage("Обновление настроек сотрудников завершилось успешно");
			}
			else {
				DbSession.Refresh(stSettings[0]);
				DbSession.Refresh(stSettings[1]);
				ErrorMessage("Возникла ошибка при обновлении настроек сотрудников");
				return View(EmployeeSettings());
			}
			return RedirectToAction("EmployeeSettings");
		}

		/// <summary>
		/// Список администраторов и создание нового
		/// </summary>
		/// <returns></returns>
		public ActionResult EmployeeList()
		{
			var employees = DbSession.Query<Employee>().OrderBy(s => s.IsDisabled).ThenBy(i => i.Name).ToList();
			ViewBag.employees = employees;
			return View();
		}

		[HttpGet]
		public ActionResult EmployeeAdd()
		{
			return View();
		}

		[HttpPost]
		public ActionResult EmployeeAdd([EntityBinder] Employee employee, string workBegin, string workEnd, string workStep)
		{
			if (!string.IsNullOrEmpty(workBegin)) {
				employee.WorkBegin = TimeSpan.Parse(workBegin, new DateTimeFormatInfo());
			}
			if (!string.IsNullOrEmpty(workEnd)) {
				employee.WorkEnd = TimeSpan.Parse(workEnd, new DateTimeFormatInfo());
			}
			if (!string.IsNullOrEmpty(workStep)) {
				employee.WorkStep = TimeSpan.Parse(workStep, new DateTimeFormatInfo());
			}
			employee.RegistrationDate = SystemTime.Now();
			var existedAgentName = DbSession.Query<Employee>()
				.Any(s => s.Name.ToLower().Replace(" ", "") == employee.Name.ToLower().Replace(" ", ""));
			var existedAgentLogin = DbSession.Query<Employee>()
				.Any(s => s.Login.ToLower().Replace(" ", "") == employee.Login.ToLower().Replace(" ", ""));
			if (!(existedAgentName || existedAgentLogin)) {
				var errors = ValidationRunner.ValidateDeep(employee);
				if (errors.Length == 0) {
					DbSession.Save(employee);
					SuccessMessage("Работник успешно добавлен");
					return RedirectToAction("EmployeeList");
				}
				else {
					ErrorMessage("Не удалось добавить сотрудника");
				}
			}
			else {
				var stMessage = "";
				if (existedAgentName && existedAgentLogin) stMessage = "и ФИО и Логином";
				if (existedAgentName && !existedAgentLogin) stMessage = " ФИО";
				if (!existedAgentName && existedAgentLogin) stMessage = " Логином";
				ErrorMessage($"Работник с подобным{stMessage} уже существует!");
			}
			ViewBag.Employee = employee;
			ViewBag.SessionToRefresh = DbSession;
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
			var permissions = DbSession.Query<Permission>().Where(s => !s.Hidden).ToList();
			_roles = roles = roles.Where(i => i != null && !employee.Roles.Any(j => j == i)).ToList();
			permissions = permissions.Where(i => !employee.Permissions.Any(j => j == i) &&
			                                     !employee.Roles.Any(s => s != null && s.Permissions.Any(k => k == i)))
				.OrderBy(s => s.Name)
				.ToList();

			ViewBag.Employee = employee;
			ViewBag.Roles = roles;
			ViewBag.Permissions = permissions;
			return View();
		}

		/// <summary>
		///Редактирование администратора
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public ActionResult EditEmployee([EntityBinder] Employee employee, string workBegin, string workEnd, string workStep,
			bool? isDisabled)
		{
			if (!string.IsNullOrEmpty(workBegin)) {
				employee.WorkBegin = TimeSpan.Parse(workBegin, new DateTimeFormatInfo());
			}
			else {
				if (workBegin == string.Empty) {
					employee.WorkBegin = null;
				}
			}
			if (!string.IsNullOrEmpty(workEnd)) {
				employee.WorkEnd = TimeSpan.Parse(workEnd, new DateTimeFormatInfo());
			}
			else {
				if (workEnd == string.Empty) {
					employee.WorkEnd = null;
				}
			}
			if (!string.IsNullOrEmpty(workStep)) {
				employee.WorkStep = TimeSpan.Parse(workStep, new DateTimeFormatInfo());
			}
			else {
				if (workStep == string.Empty) {
					employee.WorkStep = null;
				}
			}
			employee.IsDisabled = GetCurrentEmployee() != employee && (isDisabled ?? false);
			var errors = ValidationRunner.ValidateDeep(employee);
			if (errors.Length == 0) {
				DbSession.Save(employee);
				SuccessMessage("Объект успешно изменен");
			}
			else {
				ViewBag.SessionToRefresh = DbSession;
			}

			return EditEmployee(employee.Id);
		}

		/// <summary>
		/// Страница списка клиентов
		/// </summary>
		public ActionResult PaymentsForEmployee(int? id)
		{
			var pager = new InforoomModelFilter<PaymentsForEmployee>(this);

			pager.ParamDelete("itemsPerPage");
			pager.ParamSet("itemsPerPage", "9000000");
			if (id != null && id != 0) {
				var employee = DbSession.Query<Employee>().FirstOrDefault(s => s.Id == id);
				pager.ParamDelete("filter.Equal.Employee.Name");
				pager.ParamSet("filter.Equal.Employee.Name", employee.Name);
			}

			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.RegistrationDate")) &&
			    string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.RegistrationDate"))) {
				pager.ParamDelete("filter.GreaterOrEqueal.RegistrationDate");
				pager.ParamDelete("filter.LowerOrEqual.RegistrationDate");
				pager.ParamSet("filter.GreaterOrEqueal.RegistrationDate",
					SystemTime.Now().Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.RegistrationDate", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
			}

			pager.GetCriteria();
			var itemListRaw = pager.GetItems();
			var currentEmployees = itemListRaw.Select(s => s.Employee).Distinct().OrderBy(s => s.Name).ToList();
			var ItemList =
				currentEmployees.Select(
					s => new Tuple<Employee, List<PaymentsForEmployee>>(s, itemListRaw.Where(d => d.Employee == s).ToList())).ToList();

			ViewBag.Pager = pager;
			ViewBag.ItemList = ItemList;
			return View();
		}

		/// <summary>
		/// Список прав, которые можно назначать администраторам
		/// </summary>
		/// <returns></returns>
		public ActionResult PermissionList()
		{
			var rights = DbSession.Query<Permission>().Where(s => !s.Hidden).OrderBy(s => s.Name).ToList();
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

		/// <summary>
		/// Права доступа
		/// </summary>
		public ActionResult RenewActionPermissions()
		{
			EmployeePermissionViewHelper.GeneratePermissions(DbSession, this);
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
			var permissions = DbSession.Query<Permission>().Where(s => !s.Hidden).ToList();
			permissions = permissions.Where(i => !role.Permissions.Any(j => j == i)).OrderBy(s => s.Name).ToList();

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
			var pager = new InforoomModelFilter<Log>(this);
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			var criteria = pager.GetCriteria();
			ViewBag.Pager = pager;
			return View();
		}

		/// <summary>
		/// Логи
		/// </summary> 
		public ActionResult LogRegResultInfo(int Id = 0, string path = "")
		{
			var log = DbSession.Query<Log>().FirstOrDefault(s => s.Id == Id);
			ViewBag.Log = log;
			ViewBag.BackUrl = (path == string.Empty ? "/Admin/LogRegResultList" : path);
			return View();
		}
	}
}