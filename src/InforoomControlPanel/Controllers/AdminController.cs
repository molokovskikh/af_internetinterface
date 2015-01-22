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
	public class AdminController : BaseController
	{
		public AdminController()
		{
			ViewBag.BreadCrumb = "Панель управления";
		}

		public ActionResult Index()
		{
			return View();
		}

		/// <summary>
		/// Список администраторов и создание нового
		/// </summary>
		/// <returns></returns>
		public ActionResult Admins()
		{
			var newAdmin = new Administrator();
			var admins =  DbSession.Query<Administrator>().ToList();
			var employees = DbSession.Query<Employee>().OrderBy(i=>i.Name).ToList().Where(e =>admins.Where(a=>a.Employee == e).ToList().Count == 0).ToList();
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

		/// <summary>
		/// Метод, изменяющий порядок отображения сущностей.
		/// </summary>
		public ActionResult ChangeModelPriority<TModel>(int? modelId, string direction, string actionName, string controllerName)
			where TModel : IModelWithPriority, new()
		{
			var model = DbSession.Get<TModel>(modelId);
			IModelWithPriority maxIndexModel = DbSession.Query<TModel>().OrderByDescending(k => k.Priority).First();
			var maxIndex = 0;
			if(maxIndexModel != null)
				maxIndex = maxIndexModel.Priority + 1;

			IList<TModel> models;
			TModel targetModel;
			if (direction == "Up") {
				models = DbSession.Query<TModel>().OrderByDescending(k => k.Priority).ToList();
				targetModel = models.FirstOrDefault(k => k.Priority < model.Priority);
			}
			else {
				models = DbSession.Query<TModel>().OrderBy(k => k.Priority).ToList();
				targetModel = models.FirstOrDefault(k => k.Priority > model.Priority);
			}
			if (targetModel != null) {
				int targetPriority = targetModel.Priority;
				targetModel.Priority = model.Priority;
				model.Priority = maxIndex;
				DbSession.Save(targetModel);
				DbSession.Save(model);
				//сохраняем в базу, чтобы не столкнуться с Duplicate Entry
				DbSession.Flush();
				model.Priority = targetPriority;
				DbSession.Save(model);
			}
			return RedirectToAction(actionName, controllerName);
		}
	}
}