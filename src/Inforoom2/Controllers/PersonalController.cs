using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using System.Web.Razor.Parser.SyntaxTree;
using Common.MySql;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InternetInterface.Models;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.Proxy;
using AppealType = Inforoom2.Models.AppealType;
using Client = Inforoom2.Models.Client;
using ClientEndpoint = Inforoom2.Models.ClientEndpoint;
using Contact = Inforoom2.Models.Contact;
using Lease = Inforoom2.Models.Lease;
using Payment = Inforoom2.Models.Payment;
using PaymentForConnect = Inforoom2.Models.PaymentForConnect;
using PhysicalClient = Inforoom2.Models.PhysicalClient;
using Service = Inforoom2.Models.Services.Service;
using Status = Inforoom2.Models.Status;
using StatusType = Inforoom2.Models.StatusType;
using UserWriteOff = Inforoom2.Models.UserWriteOff;
using WriteOff = Inforoom2.Models.WriteOff;


namespace Inforoom2.Controllers
{
	[CustomAuthorize]
	public class PersonalController : BaseController
	{
		public ActionResult FirstVisit()
		{
			var physicalClient = DbSession.Get<PhysicalClient>(CurrentClient.PhysicalClient.Id);
			var unproxy = DbSession.GetSessionImplementation().PersistenceContext.Unproxy(physicalClient);
			ViewBag.PhysicalClient = unproxy;
			return View();
		}
		[HttpPost]
		public ActionResult FirstVisit([EntityBinder] PhysicalClient PhysicalClient)
		{
			var errors = ValidationRunner.ValidateDeep(PhysicalClient);
			if(errors.Length == 0)
			{
				DbSession.Save(PhysicalClient);
				var ip = Request.UserHostAddress;
				var address = IPAddress.Parse(ip);
#if DEBUG
				var lease = DbSession.Query<Lease>().First();
#else
				var lease = DbSession.Query<Lease>().FirstOrDefault(l => l.Ip == address);
#endif
				if(CurrentClient.Endpoints.Count == 0 && lease != null)
				{
					//var settings = new Settings(session);
					if (string.IsNullOrEmpty(lease.Switch.Name)) {
						var addr = CurrentClient.PhysicalClient.Address;
						if(addr != null)
							lease.Switch.Name = addr.House.Street.Region.City.Name + ", " + addr.House.Street.Name +", " + addr.House.Number;
					}

					var endpoint = new ClientEndpoint();
					endpoint.Client = CurrentClient;
					endpoint.Switch = lease.Switch;
					endpoint.Port = lease.Port;
					endpoint.PackageId = CurrentClient.PhysicalClient.Plan.PackageId;
					DbSession.Save(endpoint);
					lease.Endpoint = endpoint;

					var paymentForConnect = new PaymentForConnect(PhysicalClient.ConnectSum, endpoint);
					//Пытаемся найти сотрудника
					var empId = (string)Session["Employee"];
					if(empId != null)	{
						var id = uint.Parse(empId);
						var emp = DbSession.Get<Employee>(id);
						paymentForConnect.Employee = emp;
					}

					CurrentClient.SetStatus(Status.Get(StatusType.Worked, DbSession));

					var internet = CurrentClient.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Internet);
					internet.ActivateFor(CurrentClient, DbSession);
					var iptv = CurrentClient.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Iptv);
					iptv.ActivateFor(CurrentClient, DbSession);

					if (CurrentClient.IsNeedRecofiguration)
						SceHelper.UpdatePackageId(DbSession, CurrentClient);

					DbSession.Save(lease.Switch);
					DbSession.Save(paymentForConnect);
					DbSession.Save(lease);
				}
				CurrentClient.Lunched = true;
				DbSession.Save(CurrentClient);
				return RedirectToAction("Profile");
			}
			ViewBag.PhysicalClient = PhysicalClient;
			return View();
		}

		public new ActionResult Profile()
		{
			if(CurrentClient == null)
				return RedirectToAction("Login", "Account");

			//if(CurrentClient.Lunched == false)
			//	return RedirectToAction("FirstVisit");

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
			}).ToList());

			historyList.AddRange(payments.Select(payment => new BillingHistory {
				Date = payment.RecievedOn,
				Sum = payment.Sum,
				Comment = payment.Comment,
				WhoRegistered = (payment.Virtual.HasValue && payment.Virtual.Value) ? "Инфорум" : "",
				Description = new StringBuilder().AppendFormat("Пополнение счета").ToString()
			}).ToList());

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
					var appeal = new Appeal("Клиент отписался от смс рассылки", client, AppealType.User);
					DbSession.Save(appeal);
					SuccessMessage("Вы успешно отписались от смс рассылки");
				}
				else {
					client.SendSmsNotification = true;
					var appeal = new Appeal("Клиент подписался смс рассылку", client, AppealType.User);
					DbSession.Save(appeal);
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
			var oldPlan = client.PhysicalClient.Plan;
			var result = client.PhysicalClient.ChangeTariffPlan(plan);
			if (result == null) {
				ErrorMessage("Не достаточно средств для смены тарифного плана");
				InitPlans(client);
				return View("Tariffs");
			}
			DbSession.Save(client);
			DbSession.Save(result);
			SuccessMessage("Тариф изменен");
			var appeal = new Appeal("Тарифный план был изменен с " + oldPlan.Name + " на " + plan.Name, client, AppealType.User);
			DbSession.Save(appeal);
			return RedirectToAction("Tariffs");
		}

		protected decimal GetPlanSwitchPrice(Plan planFrom, Plan planTo, bool onlyAvailableToSwitch)
		{
			IList<PlanTransfer> prices = planFrom.PlanTransfers;
			var price = prices.FirstOrDefault(p => p.PlanFrom.Id == planFrom.Id && p.PlanTo.Id == planTo.Id);
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