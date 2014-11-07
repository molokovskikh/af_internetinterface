using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	[Authorize]
	public class PersonalController : BaseController
	{
		public new ActionResult Profile()
		{
			ViewBag.Title = "Личный кабинет";
			ViewBag.CurrentClient = CurrentClient;
			return View();
		}

		public ActionResult Tariffs()
		{
			var client = CurrentClient;
			var plans = Plans.Where(p => !p.IsArchived && !p.IsServicePlan && p.Regions.Any(r => r.Id == client.Address.House.Street.Region.Id)).ToList();
			foreach (var plan in plans) {
				plan.SwitchPrice = GetPlanSwitchPrice(client.Plan, plan, true);
			}

			plans = plans.Where(p => p.SwitchPrice != -1).ToList();
			ViewBag.Plans = plans;
			ViewBag.Title = "Тарифы";
			ViewBag.CurrentClient = client;

			return View();
		}

		public ActionResult Payment()
		{
			ViewBag.Title = "Платежи";
			return View();
		}

		public ActionResult Credit()
		{
			ViewBag.Title = "Доверительный платеж";
			return View();
		}

		public ActionResult UserDetails()
		{
			ViewBag.Title = "Данные пользователя";
		return View();
		}

		public ActionResult Service()
		{
			ViewBag.Title = "Услуги";
		return View();
		}

		public ActionResult Notifications()
		{
			ViewBag.Title = "Уведомления";
			return View();
		}

		public ActionResult Bonus()
		{
			ViewBag.Title = "Бонусы";
			return View();
		}

		[HttpPost]
		public ActionResult ChangePlan([EntityBinder] Plan plan)
		{
		//	var plan = DbSession.Get<Plan>(planId);
			var client = CurrentClient;
			plan.SwitchPrice = GetPlanSwitchPrice(client.Plan, plan, true);
			if (!client.ChangeTariffPlan(plan)) {
				ErrorMessage("Не достаточно средств для смены тарифного плана");
				return View("Tariffs");
			}
			DbSession.Save(client);
			SuccessMessage("Тариф изменен");
			return RedirectToAction("Tariffs");
		}

		protected decimal GetPlanSwitchPrice(Plan planFrom, Plan planTo, bool onlyAvailableToSwitch)
		{
			IList<PlanTransfer> prices = planFrom.PlanTransfers;
			var price = prices.FirstOrDefault(p => p.PlanFrom.Id == planFrom.Id
			                                       && p.PlanTo.Id == planTo.Id);
			if (price != null) {
				return price.Price;
			}
			return -1;
		}
	}
}