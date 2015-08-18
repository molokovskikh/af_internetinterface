using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	public class ControlPanelController : BaseController
	{
		//@todo вынести в SecuredController - общего для информа и админки, а то получается дублирование кода
		protected override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			base.OnActionExecuting(filterContext);
			if (filterContext == null)
				throw new ArgumentNullException("filterContext");

			//если клиент был залогинен по сети, то HTTPСontext не будет изменен
			//в этом случае можно оттолкнуть от переменной CurrentClient
			if (!filterContext.HttpContext.User.Identity.IsAuthenticated) {
				string loginUrl = "/AdminAccount/Index"; // Default Login Url 
				filterContext.Result = new RedirectResult(loginUrl);
				return;
			}

			var employee = GetCurrentEmployee();
			ModelCrudListener.SetEmployee(employee);
			string name = ViewBag.ControllerName + "Controller_" + ViewBag.ActionName;
			var permission = DbSession.Query<Permission>().FirstOrDefault(i => i.Name == name);
			//@todo убрать проверку, на accessDenined, а вместо этого просто не генерировать его. В целом подумать
			if (permission != null && permission.Name != "AdminController_AccessDenined" && (employee == null || !employee.HasAccess(permission.Name)))
				filterContext.Result = new RedirectResult("/Admin/AccessDenined");
		}

		public override Employee GetCurrentEmployee()
		{
			if (User == null || DbSession == null || !DbSession.IsConnected) {
				return null;
			}
			return DbSession.Query<Employee>().FirstOrDefault(e => e.Login == User.Identity.Name);
		}

		protected override void OnResultExecuting(ResultExecutingContext filterContext)
		{
			var curEmployee = GetCurrentEmployee();
			ViewBag.CurrentEmployee = curEmployee ?? new Employee();
		}

		/// <summary>
		/// Метод, изменяющий порядок отображения сущностей.
		/// </summary>
		//todo этот метод надо как то поменять
		public ActionResult ChangeModelPriority<TModel>(int? modelId, string direction, string actionName, string controllerName)
			where TModel : IModelWithPriority, new()
		{
			var model = DbSession.Get<TModel>(modelId);
			IModelWithPriority maxIndexModel = DbSession.Query<TModel>().OrderByDescending(k => k.Priority).First();
			var maxIndex = 0;
			if (maxIndexModel != null)
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