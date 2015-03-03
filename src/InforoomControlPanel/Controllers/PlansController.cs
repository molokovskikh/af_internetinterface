using System;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;
using NHibernate.Util;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления вопросами от пользователя
	/// </summary>
	public class PlansController : InforoomControlPanel.Controllers.AdminController
	{
		/// <summary>
		/// Список тарифов
		/// </summary>
		public ActionResult PlanIndex()
		{
			var plans = DbSession.Query<Plan>().OrderByDescending(i=> i.Id).ToList();
			ViewBag.Plans = plans;
			return View();
		}

		/// <summary>
		/// Просмотр тарифа
		/// </summary>
		public ActionResult EditPlan(int id)
		{
			var plan = DbSession.Get<Plan>(id);
			var PlanTransfer = new PlanTransfer();
			PlanTransfer.PlanFrom = plan;
			var plans = DbSession.Query<Plan>().OrderByDescending(i => i.Id).ToList();
			foreach (var transfer in plan.PlanTransfers) {
				plans.Remove(transfer.PlanTo);
			}

			ViewBag.Plans = plans;
			ViewBag.Plan = plan;
			ViewBag.PlanTransfer = PlanTransfer;
			return View("EditPlan");
		}

		/// <summary>
		/// Изменение тарифа
		/// </summary>
		public ActionResult UpdatePlan([EntityBinder] Plan plan)
		{
			var errors = ValidationRunner.ValidateDeep(plan);
			if (errors.Length == 0) {
				DbSession.Save(plan);
				SuccessMessage("Тарифный план успешно отредактирован");
				return RedirectToAction("EditPlan", new { id = plan.Id });
			}

			EditPlan(plan.Id);
			ViewBag.Plan = plan;
			return View("EditPlan");
		}

		/// <summary>
		/// Добавление стоимости перехода
		/// </summary>
		public ActionResult AddPlanTransfer([EntityBinder] PlanTransfer planTransfer)
		{
			var errors = ValidationRunner.ValidateDeep(planTransfer);
			if (errors.Length == 0)
			{
				DbSession.Save(planTransfer);
				SuccessMessage("Стоимость перехода успешно отредактирован");
				return RedirectToAction("EditPlan", new { id = planTransfer.PlanFrom.Id });
			}
			EditPlan(planTransfer.PlanFrom.Id);
			ViewBag.PlanTransfer = planTransfer;
			return View("EditPlan");
		}

		/// <summary>
		/// Удаление стоимости перехода
		/// </summary>
		public ActionResult DeletePlanTransfer(int id)
		{
			var transfer = DbSession.Get<PlanTransfer>(id);
			DbSession.Delete(transfer);
			DbSession.Flush();
			SuccessMessage("Стоимость перехода успешно удалена");
			return RedirectToAction("EditPlan", new { id = transfer.PlanFrom.Id });
		}
	}
}