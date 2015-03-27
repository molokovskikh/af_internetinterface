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
	public class PlansController : AdminController
	{
		/// <summary>
		/// Список тарифов
		/// </summary>
		public ActionResult PlanIndex()
		{
			var plans = DbSession.Query<Plan>().OrderByDescending(i => i.Id).ToList();
			var regions = DbSession.Query<Region>().ToList();
			ViewBag.Plans = plans;
			ViewBag.Regions = regions;
			return View();
		}

		/// <summary>
		/// Форма добавление тарифа
		/// </summary>
		public ActionResult CreatePlan()
		{
			//Создаентся тарифный план  
			var plan = new Plan();
			ViewBag.Plan = plan;
			ViewBag.PackageSpeed = DbSession.Query<PackageSpeed>().OrderBy(s => s.Speed)
				.GroupBy(s => s.Speed).Select(grp => grp.First()).ToList();

			return View();
		}
		/// <summary>
		/// Добавление тарифа в базу
		/// </summary>
		[HttpPost]
		public ActionResult CreatePlan([EntityBinder] Plan plan)
		{
			var errors = ValidationRunner.ValidateDeep(plan);
			if (errors.Length == 0)
			{
				DbSession.Save(plan);
				SuccessMessage("Тарифный план успешно добавлен!");
				return RedirectToAction("PlanIndex");
			}
			ViewBag.Plan = plan;
			ViewBag.PackageSpeed = DbSession.Query<PackageSpeed>().OrderBy(s => s.Speed)
				.GroupBy(s => s.Speed).Select(grp => grp.First()).ToList();

			return View("CreatePlan");
		}
		
		/// <summary>
		/// Просмотр тарифа
		/// </summary>
		public ActionResult EditPlan(int id)
		{
			//забирается тарифный план из базы данных
			var plan = DbSession.Get<Plan>(id);
			//создание промежуточного объекта для перехода между тарифами
			var PlanTransfer = new PlanTransfer();
			//назначение поля тарифа
			PlanTransfer.PlanFrom = plan;
			var plans = DbSession.Query<Plan>().OrderByDescending(i => i.Id).ToList();
			foreach (var transfer in plan.PlanTransfers)
			{
				plans.Remove(transfer.PlanTo);
			}

			var RegionPlan = new RegionPlan();
			RegionPlan.Plan = plan;
			var regions = DbSession.Query<Region>().ToList();
			foreach (var rp in plan.RegionPlans)
			{
				regions.Remove(rp.Region);
			}
			ViewBag.PackageSpeed = DbSession.Query<PackageSpeed>().OrderBy(s => s.Speed)
				.GroupBy(s => s.Speed).Select(grp => grp.First()).ToList();

			ViewBag.Plans = plans;
			ViewBag.Plan = plan;
			ViewBag.Regions = regions;
			ViewBag.PlanTransfer = PlanTransfer;
			ViewBag.RegionPlan = RegionPlan;
			return View("EditPlan");
		}

		/// <summary>
		/// Изменение тарифа
		/// </summary>
		public ActionResult UpdatePlan([EntityBinder] Plan plan)
		{
			var errors = ValidationRunner.ValidateDeep(plan);
			if (errors.Length == 0)
			{
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

		/// <summary>
		/// Добавление региона
		/// </summary>
		/// <returns></returns>
		public ActionResult AddRegionPlan([EntityBinder] RegionPlan regionPlan)
		{
			var errors = ValidationRunner.Validate(regionPlan);
			if (errors.Length == 0)
			{
				DbSession.Save(regionPlan);
				SuccessMessage("Регион успешно добавлен");
				return RedirectToAction("EditPlan", new { id = regionPlan.Plan.Id });
			}
			EditPlan(regionPlan.Plan.Id);
			ViewBag.RegionPlan = regionPlan;
			return View("EditPlan");
		}


		/// <summary>
		/// Удаление региона
		/// </summary>
		public ActionResult DeleteRegion(int id)
		{
			var rp = DbSession.Get<RegionPlan>(id);
			DbSession.Delete(rp);
			DbSession.Flush();
			SuccessMessage("Регион успешно удален");
			return RedirectToAction("EditPlan", new { id = rp.Plan.Id });
		}



	}
}