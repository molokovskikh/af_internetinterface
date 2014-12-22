using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
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
		//public ActionResult Index()
		//{
		//	//проверяем наличие неотвеченных тикетов
		//	ViewBag.TicketsAmount = DbSession.Query<Ticket>().Count(k => !k.IsNotified);
		//	return View();
		//}


		/// <summary>
		/// Метод, изменяющий порядок отображения сущностей.
		/// </summary>
		public ActionResult ChangeModelPriority<TModel>(int? modelId, string direction, string actionName, string controllerName)
			where TModel : IModelWithPriority, new()
		{
			var model = DbSession.Get<TModel>(modelId);
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
				int targetPerriority = targetModel.Priority;
				targetModel.Priority = model.Priority;
				model.Priority = 0;
				DbSession.Save(targetModel);
				DbSession.Save(model);
				//сохраняем в базу, чтобы не столкнуться с Duplicate Entry
				DbSession.Flush();
				model.Priority = targetPerriority;
				DbSession.Save(model);
			}
			return RedirectToAction(actionName, controllerName);
		}
	}
}