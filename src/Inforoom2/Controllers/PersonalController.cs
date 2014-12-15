using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;
using Common.MySql;
using Inforoom2.Components;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Proxy;
using Client = Inforoom2.Models.Client;
using Contact = Inforoom2.Models.Contact;
using Payment = Inforoom2.Models.Payment;
using Service = Inforoom2.Models.Services.Service;
using UserWriteOff = Inforoom2.Models.UserWriteOff;
using WriteOff = Inforoom2.Models.WriteOff;


namespace Inforoom2.Controllers
{
	[Authorize]
	public class PersonalController : BaseController
	{
		public new ActionResult Profile()
		{
			InitServices();
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
			var client = CurrentClient;
			var writeOffs = DbSession.Query<WriteOff>().Where(wo => wo.Client.Id == client.Id && wo.WriteOffDate > DateTime.Now.AddMonths(-3));
			var userWriteOffs = DbSession.Query<UserWriteOff>().Where(uwo => uwo.Client.Id == client.Id && uwo.Date > DateTime.Now.AddMonths(-3));
			var payments = DbSession.Query<Payment>().Where(p => p.Client.Id == client.Id && p.RecievedOn > DateTime.Now.AddMonths(-3));

			var historyList = userWriteOffs.Select(userWriteOff => new BillingHistory {
				Date = userWriteOff.Date,
				Sum = userWriteOff.Sum,
				Description = userWriteOff.Comment
			}).ToList();

			historyList.AddRange(writeOffs.Select(writeOff => new BillingHistory {
				Date = writeOff.WriteOffDate,
				Sum = writeOff.WriteOffSum,
				Description = new StringBuilder().AppendFormat("Абонентская плата").ToString()
			}));

			historyList.AddRange(payments.Select(payment => new BillingHistory {
				Date = payment.RecievedOn,
				Sum = payment.Sum,
				Description = new StringBuilder().AppendFormat("Пополнение счета").ToString()
			}));

			historyList = historyList.OrderByDescending(h => h.Date).ToList();
			ViewBag.HistoryList = historyList;
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
			var client = CurrentClient;
			var smsContact = client.Contacts.FirstOrDefault(c => c.Type == ContactType.SmsSending);
			if (smsContact == null) {
				smsContact = new Contact();
				smsContact.Type = ContactType.SmsSending;
			}
			ViewBag.Contact = smsContact;
			ViewBag.IsSmsNotificationActive = client.SendSmsNotification;
			ViewBag.Title = "Уведомления";
			return View();
		}


		[HttpPost]
		public ActionResult Notifications(Contact contact)
		{
			var client = CurrentClient;
			var errors = ValidationRunner.ValidateDeep(contact);
			if (errors.Length == 0) {
				var smsContact = client.Contacts.FirstOrDefault(c => c.Type == ContactType.SmsSending);
				if (smsContact == null) {
					smsContact = contact;
					smsContact.Date = DateTime.Now;
					smsContact.Comment = "Пользователь создал из личного кабинета";
					client.Contacts.Add(smsContact);
				}
				if (client.SendSmsNotification) {
					client.SendSmsNotification = false;
					SuccessMessage("Вы успешно отписались от смс рассылки");
				}
				else {
					client.SendSmsNotification = true;
					SuccessMessage("Вы успешно подписались на смс рассылку");
				}

				smsContact.ContactString = contact.ContactString;
				DbSession.Save(client);
				return RedirectToAction("Notifications");
			}

			ViewBag.Contact = contact;
			ViewBag.IsSmsNotificationActive = client.SendSmsNotification;
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
			return 0;
		}

		protected void InitServices()
		{
			var client = CurrentClient;
			var services = DbSession.Query<Service>().Where(s => s.IsActivableFromWeb);
			var blockAccountService = services.OfType<BlockAccountService>().FirstOrDefault();
			var deferredPayment = services.OfType<DeferredPayment>().FirstOrDefault();
			var pinnedIp = services.OfType<PinnedIp>().FirstOrDefault();
			var inforoomServices = new List<Service> { blockAccountService, deferredPayment };

			ViewBag.Client = client;
			ViewBag.ClientServices = client.ClientServices.Where(cs => cs.Service.IsActivableFromWeb && cs.IsActivated).ToList();
			ViewBag.AvailableServices = inforoomServices;

			ViewBag.BlockAccountService = blockAccountService;
			ViewBag.DeferredPayment = deferredPayment;
		}

		private void InitPlans(Client client)
		{
			IList<Plan> plans = null;
			//если адреса нет, показываем все тарифы
			if (client.PhysicalClient.Address != null) {
				plans = GetList<Plan>().Where(p => !p.IsArchived && !p.IsServicePlan && p.Regions.Any(r => r.Id == client.PhysicalClient.Address.House.Street.Region.Id)).ToList();
			}
			else {
				plans = GetList<Plan>().Where(p => !p.IsArchived && !p.IsServicePlan).ToList();
			}

			foreach (var plan in plans) {
				plan.SwitchPrice = GetPlanSwitchPrice(client.PhysicalClient.Plan, plan, true);
			}
			ViewBag.Plans = plans.ToList();
		}
	}
}