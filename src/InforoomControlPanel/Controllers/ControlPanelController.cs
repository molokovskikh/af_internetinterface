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
using System.Web.Security;
using System.Security.Principal;

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

			var employee = GetCurrentEmployee();

			//если клиент был залогинен по сети, то HTTPСontext не будет изменен
			//в этом случае можно оттолкнуть от переменной CurrentClient
			if (!filterContext.HttpContext.User.Identity.IsAuthenticated || employee == null) {
				string loginUrl = Url.Action("Index", "AdminAccount"); // Default Login Url 
				filterContext.Result = new RedirectResult(loginUrl);
				return;
			}
			
			string name = ViewBag.ControllerName + "Controller_" + ViewBag.ActionName;
			var permission = DbSession.Query<Permission>().FirstOrDefault(i => i.Name == name);
			//@todo убрать проверку, на accessDenined, а вместо этого просто не генерировать его. В целом подумать
			if (permission != null && permission.Name != "AdminController_AccessDenined" &&
				(!employee.HasAccess(permission.Name) || employee.IsDisabled)) {
				log.Error($"Пользователь '{employee.Name}' ({employee.Id}) попытался получить доступ к '{permission.Description}' ({permission.Id}, {permission.Name}), не имея на это прав.");
			filterContext.Result = new RedirectResult(Url.Action("AccessDenined","Admin"));
			}
		}

		public override Employee GetCurrentEmployee()
		{
			if (User == null || DbSession == null || !DbSession.IsConnected) {
				return null;
			}  
      return DbSession.Query<Employee>().FirstOrDefault(e => e.Login == SecurityContext.CurrentEmployeeName);
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

		protected override void OnException(ExceptionContext filterContext)
		{
			if (DbSession == null)
				return;
			try {
				if (DbSession.Transaction.IsActive)
					DbSession.Transaction.Rollback();
				if (DbSession.IsOpen)
					DbSession.Close();
			} catch (Exception) {
				//не реагируем на ошибку при закрытии сессии
			}
		}

		/// <summary>
		/// Откат транзакции
		/// </summary>
		public void PreventSessionUpdate()
		{
			ViewBag.SessionToRefresh = DbSession;
		}
	}
}