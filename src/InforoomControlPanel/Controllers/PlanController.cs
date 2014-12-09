﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	public class PlanController : AdminController
	{
		public ActionResult Plan()
		{
			var plans = DbSession.Query<Plan>().ToList();
			if (plans.Count == 0) {
				plans = new List<Plan>();
			}
			ViewBag.Plans = plans;
			return View();
		}

		[HttpPost]
		public ActionResult DeletePlan([EntityBinder] Plan plan)
		{
			DbSession.Delete(plan);
			return RedirectToAction("Plan");
		}

		public ActionResult EditPlan(int? planId)
		{
			Plan plan;
			if (planId != null) {
				plan = DbSession.Get<Plan>(planId);
			}
			else {
				plan = new Plan();
				plan.Regions = new List<Region>();
			}
			ViewBag.Plan = plan;
			ViewBag.Regions = GetList<Region>();
			return View();
		}

		public ActionResult UpdatePlan([EntityBinder] Plan plan, string[] checkedRegions)
		{
			if (plan.Regions == null) {
				plan.Regions = new List<Region>();
			}
			plan.Regions.Clear();
			plan.PlanTransfers = new List<PlanTransfer>();
			if (checkedRegions != null) {
				foreach (var checkedRegion in checkedRegions) {
					var region = DbSession.Get<Region>(Convert.ToInt32(checkedRegion));
					plan.Regions.Add(region);
				}
			}
			var errors = ValidationRunner.ValidateDeep(plan);
			if (errors.Length == 0) {
				DbSession.SaveOrUpdate(plan);
				SuccessMessage("Тариф успешно отредактирован");
			}
			else {
				ViewBag.Plan = plan;
				ViewBag.Regions = GetList<Region>();
				return View("EditPlan");
			}

			return RedirectToAction("Plan");
		}
	}
}