using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;
using Common.MySql;
using Inforoom2.Components;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Proxy;
using Service = Inforoom2.Models.Services.Service;


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
			InitPlans(client);
			ViewBag.Title = "Тарифы";
			ViewBag.Client = client;

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
			InitServices();
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
			ViewBag.Client = client;
			plan.SwitchPrice = GetPlanSwitchPrice(client.PhysicalClient.Plan, plan, true);
			var result = client.PhysicalClient.ChangeTariffPlan(plan);
			if (result == null) {
				ErrorMessage("Не достаточно средств для смены тарифного плана");
				InitPlans(client);
				return View("Tariffs");
			}
			DbSession.Save(client);
			DbSession.Save(result);
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

		protected void InitServices()
		{
			var client = CurrentClient;
			var services = DbSession.Query<Service>().Where(s => s.IsActivableFromWeb);
			var blockAccountService = services.OfType<BlockAccountService>().FirstOrDefault();
			var deferredPayment = services.OfType<DeferredPayment>().FirstOrDefault();
			var pinnedIp = services.OfType<PinnedIp>().FirstOrDefault();
			var inforoomServices = new List<Service> {blockAccountService, deferredPayment};
			inforoomServices = inforoomServices.Where(i => i.IsActivableFor(client)).ToList();
			ViewBag.Client = client;
			ViewBag.ClientServices = client.ClientServices.Where(cs => cs.Service.IsActivableFromWeb && cs.IsActivated).ToList();
			ViewBag.AvailableServices = inforoomServices;

			ViewBag.BlockAccountService = blockAccountService;
			ViewBag.DeferredPayment = deferredPayment;
		}

		private void InitPlans(Client client)
		{
			var plans =
				Plans.Where(
					p =>
						!p.IsArchived && !p.IsServicePlan &&
						p.Regions.Any(r => r.Id == client.PhysicalClient.Address.House.Street.Region.Id)).ToList();
			foreach (var plan in plans) {
				plan.SwitchPrice = GetPlanSwitchPrice(client.PhysicalClient.Plan, plan, true);
			}

			plans = plans.Where(p => p.SwitchPrice != -1).ToList();
			ViewBag.Plans = plans;
		}
	}
}