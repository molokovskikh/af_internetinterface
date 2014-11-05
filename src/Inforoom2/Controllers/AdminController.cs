using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Common.MySql;
using Inforoom2.Helpers;
using Inforoom2.Intefaces;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Базовый контролер администратора
	/// </summary>
	[AuthorizeUser(Roles = "Admin")]
	public class AdminController : BaseController
	{
		protected List<City> Cities
		{
			get { return GetAllSafe<City>(); }
		}

		protected List<Street> Streets
		{
			get { return GetAllSafe<Street>(); }
		}

		protected IList<Region> Regions
		{
			get { return GetAllSafe<Region>(); }
		}

		protected IList<House> Houses
		{
			get { return GetAllSafe<House>(); }
		}

		protected IList<Address> Addresses
		{
			get { return GetAllSafe<Address>(); }
		}

		protected IList<SwitchAddress> SwitchAddresses
		{
			get { return GetAllSafe<SwitchAddress>(); }
		}

		protected IList<Switch> Switches
		{
			get { return GetAllSafe<Switch>(); }
		}

		public ActionResult Index()
		{
			//проверяем наличие неотвеченных тикетов
			ViewBag.TicketsAmount = DbSession.Query<Ticket>().Count(k => !k.IsNotified);
			return View();
		}


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