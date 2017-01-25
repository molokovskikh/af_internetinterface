using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления новостями
	/// </summary>
	public class EmployeeGroupController : ControlPanelController
	{
		public ActionResult Index()
		{
			ViewBag.CurrentGroup = new EmployeeGroup();
			ViewBag.EmployeeGroupList = DbSession.Query<EmployeeGroup>().OrderBy(s=>s.Name).ToList();
			return View();
		}
		
		[HttpPost]
		public ActionResult GroupAdd(string name)
		{
			if (string.IsNullOrEmpty(name)) {
				ErrorMessage("Для добавления новой группы необходимо указать ее наименование.");
				return RedirectToAction("Index");
			} else if (
				DbSession.Query<EmployeeGroup>()
					.ToList()
					.Any(s => s.Name.Replace(" ", "").ToLower() == name?.Replace(" ", "").ToLower())) {
				ErrorMessage("Группа с указанным наименованием уже существует.");
			} else {
				var newGroup = new EmployeeGroup() {Name = name};
				DbSession.Save(newGroup);
				SuccessMessage("Группа успешно добавлена.");
			}
			return RedirectToAction("Index");
		}

		[HttpGet]
		public ActionResult GroupEdit(int? id)
		{
			var currentGroup = DbSession.Query<EmployeeGroup>().FirstOrDefault(s => s.Id == id);
			if (currentGroup == null) {
				ErrorMessage("Группа с данным идентификатором не существует.");
				return RedirectToAction("Index");
			}
			ViewBag.CurrentGroup = currentGroup;
			ViewBag.EmployeeList = DbSession.Query<Employee>().Where(s => !s.IsDisabled).OrderBy(s => s.Name).ToList()
				.Where(s => currentGroup.EmployeeList.All(f => f.Id != s.Id)).ToList();
			return View();
		}

		[HttpPost]
		public ActionResult GroupEdit(int? id, string name)
		{
			var currentGroup = DbSession.Query<EmployeeGroup>().FirstOrDefault(s => s.Id == id);
			if (currentGroup == null) {
				ErrorMessage("Группа с данным идентификатором не существует.");
			} else if (string.IsNullOrEmpty(name)) {
				ErrorMessage("Для обновления группы необходимо указать ее новое наименование.");
			} else if (
				DbSession.Query<EmployeeGroup>().Where(s => s.Id != id).ToList()
					.Any(s => s.Name.Replace(" ", "").ToLower() == name?.Replace(" ", "").ToLower())) {
				ErrorMessage("Группа с указанным наименованием уже существует.");
			} else {
				currentGroup.Name = name;
				DbSession.Save(currentGroup);
				SuccessMessage("Группа успешно обновлена.");
			}
			ViewBag.CurrentGroup = currentGroup;
			ViewBag.EmployeeList = DbSession.Query<Employee>().Where(s => !s.IsDisabled).OrderBy(s => s.Name).ToList();
			return View();
		}
		
		public ActionResult GroupDelete(int? id)
		{
			var currentGroup = DbSession.Query<EmployeeGroup>().FirstOrDefault(s => s.Id == id);
			if (currentGroup == null) {
				ErrorMessage("Группа с данным идентификатором не существует.");
			} else if (currentGroup.EmployeeList.Count > 0) {
				ErrorMessage("Невозможно удалить группу: в группе до сих пор есть участники.");
			} else {
				DbSession.Delete(currentGroup);
				SuccessMessage("Группа успешно удалена.");
			}
			return RedirectToAction("Index");
		}

		public ActionResult EmployeeToGroupAdd(int? groupId, int? employeeId)
		{
			var currentGroup = DbSession.Query<EmployeeGroup>().FirstOrDefault(s => s.Id == groupId);
			var currentEmployee = DbSession.Query<Employee>().FirstOrDefault(s => s.Id == employeeId);
			if (currentGroup == null) {
				ErrorMessage($"Группа с данным идентификатором не существует: {groupId}.");
				return RedirectToAction("Index");
			} else if (currentEmployee == null) {
				ErrorMessage($"Пользователя с данным идентификатором не существует: {employeeId}.");
			} else if (currentGroup.EmployeeList.Any(s=>s.Id == employeeId)) {
				ErrorMessage($"Пользователь {currentEmployee.Name} уже состоить в группе {currentGroup.Name}.");
			} else {
				currentGroup.EmployeeList.Add(currentEmployee);
				DbSession.Save(currentGroup);
				SuccessMessage($"Сотрудник {currentEmployee.Name} вошел в группу {currentGroup.Name}.");
			}
			return RedirectToAction("GroupEdit",new {id = groupId });
		}

		public ActionResult EmployeeToGroupDelete(int? groupId, int? employeeId)
		{
			var currentGroup = DbSession.Query<EmployeeGroup>().FirstOrDefault(s => s.Id == groupId);
			var currentEmployee = DbSession.Query<Employee>().FirstOrDefault(s => s.Id == employeeId);
			if (currentGroup == null) {
				ErrorMessage($"Группа с данным идентификатором не существует: {groupId}.");
				return RedirectToAction("Index");
			} else if (currentEmployee == null) {
				ErrorMessage($"Пользователя с данным идентификатором не существует: {employeeId}.");
			} else {
				currentGroup.EmployeeList = currentGroup.EmployeeList.Where(s => s.Id != employeeId).ToList();
				DbSession.Save(currentGroup);
				SuccessMessage($"Сотрудник {currentEmployee.Name} больше не состоит в группе {currentGroup.Name}.");
			}
			return RedirectToAction("GroupEdit", new { id = groupId });
		}
		
	}
}