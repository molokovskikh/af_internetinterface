﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InternetInterface.Models;
using NHibernate.Linq;
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
	public class PersonalController : Inforoom2Controller
	{
		public ActionResult FirstVisit()
		{
			if (CurrentClient.Lunched && CurrentClient.HasPassportData())
				return RedirectToAction("Profile");
			var physicalClient = DbSession.Get<PhysicalClient>(CurrentClient.PhysicalClient.Id);
			//TODO Придумать что с этим делать
			var unproxy = DbSession.GetSessionImplementation().PersistenceContext.Unproxy(physicalClient);
			ViewBag.PhysicalClient = unproxy;
			return View();
		}

		[HttpPost]
		public ActionResult FirstVisit([EntityBinder] PhysicalClient physicalClient)
		{
			var errors = ValidationRunner.Validate(physicalClient);
			if (errors.Length == 0) {
				DbSession.Save(physicalClient);
				var ip = Request.UserHostAddress;
				var address = IPAddress.Parse(ip);
#if DEBUG
				var lease = DbSession.Query<Lease>().First();
#else
				var lease = DbSession.Query<Lease>().FirstOrDefault(l => l.Ip == address);
#endif
				if (CurrentClient.Endpoints.Count == 0 && lease != null) {
					//var settings = new Settings(session);
					if (string.IsNullOrEmpty(lease.Switch.Name)) {
						var addr = CurrentClient.PhysicalClient.Address;
						if (addr != null)
							lease.Switch.Name = addr.House.Street.Region.City.Name + ", " + addr.House.Street.Name + ", " + addr.House.Number;
						else
							lease.Switch.Name = CurrentClient.Id + ": адрес неопределен";
					}

					var endpoint = new ClientEndpoint();
					endpoint.Client = CurrentClient;
					endpoint.Switch = lease.Switch;
					endpoint.Port = lease.Port;
					endpoint.PackageId = CurrentClient.PhysicalClient.Plan.PackageSpeed.PackageId;
					DbSession.Save(endpoint);
					lease.Endpoint = endpoint;

					var paymentForConnect = new PaymentForConnect(physicalClient.ConnectSum, endpoint);
					//Пытаемся найти сотрудника
					paymentForConnect.Employee = GetCurrentEmployee();

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
				SuccessMessage("Данные успешно заполнены");
				CurrentClient.Lunched = true;
				DbSession.Save(CurrentClient);
				return RedirectToAction("Profile");
			}
			ViewBag.PhysicalClient = physicalClient;
			return View();
		}

		public new ActionResult Profile()
		{
			if (CurrentClient == null)
				return RedirectToAction("Login", "Account");

			if (!CurrentClient.Lunched)
				return RedirectToAction("FirstVisit");

			InitServices();
			ViewBag.Title = "Личный кабинет";
			ViewBag.CurrentClient = CurrentClient;
			return View();
		}

		public ActionResult Plans()
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
			var userWriteOffs = DbSession.Query<UserWriteOff>().Where(uwo => uwo.Client.Id == client.Id && uwo.Date > DateTime.Now.AddMonths(-3));
			var writeOffs = DbSession.Query<WriteOff>().Where(wo => wo.Client.Id == client.Id && wo.WriteOffDate > DateTime.Now.AddMonths(-3));
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

			var paymentsList = new List<BillingHistory>();
			foreach (var payment in payments) {
				var obj = new BillingHistory();
				obj.Date = payment.RecievedOn;
				obj.Sum = payment.Sum;
				obj.Comment = payment.Comment;
				obj.WhoRegistered = "Инфорум";
				if (payment.Employee != null && payment.Employee.IsPaymentSystem())
					obj.WhoRegistered = payment.Employee.Name;
				if (payment.Virtual.HasValue && payment.Virtual.Value)
					obj.WhoRegistered += " (бонус)";
				obj.Description = new StringBuilder().AppendFormat("Пополнение счета").ToString();
				paymentsList.Add(obj);
			}
			historyList.AddRange(paymentsList);

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
			var errors = ValidationRunner.Validate(contact);
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
					var appeal = new Appeal("Клиент отписался от смс рассылки", client, AppealType.User) {
						Employee = GetCurrentEmployee()
					};
					DbSession.Save(appeal);
					SuccessMessage("Вы успешно отписались от смс рассылки");
				}
				else {
					client.SendSmsNotification = true;
					var appeal = new Appeal("Клиент подписался на смс рассылку", client, AppealType.User) {
						Employee = GetCurrentEmployee()
					};
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

		/// <summary>
		/// Смена тарифного плана
		/// </summary>
		/// <param name="plan">Тарифный план</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult ChangePlan([EntityBinder] Plan plan)
		{
			var client = CurrentClient;
			InitPlans(client);
			ViewBag.Client = client;
			//todo - наверно надо подумать как эти провеки засунуть куда следует
			var beginDate = client.WorkingStartDate ?? new DateTime();
			if (beginDate == DateTime.MinValue || beginDate.AddMonths(2) >= DateTime.Now) {
				ErrorMessage("Нельзя менять тариф, в первые 2 месяца после подключения");
				return View("Plans");
			}

			var oldPlan = client.PhysicalClient.Plan;
			var result = client.PhysicalClient.RequestChangePlan(plan);
			if (result == null) {
				ErrorMessage("Не достаточно средств для смены тарифного плана");
				return View("Plans");
			}

			DbSession.Save(client);
			DbSession.Save(result);
			var warning = (client.GetWorkDays() <= 3) ? " Обратите внимание, что у вас низкий баланс!" : "";
			SuccessMessage("Тариф успешно изменен." + warning);
			var msg = string.Format("Изменение тарифа был изменен с '{0}'({1}) на '{2}'({3}). Стоимость перехода: {4} руб.", oldPlan.Name, oldPlan.Price, plan.Name, plan.Price, result.Sum);
			var appeal = new Appeal(msg, client, AppealType.User) {
				Employee = GetCurrentEmployee()
			};
			DbSession.Save(appeal);
			return RedirectToAction("Plans");
		}


		protected void InitServices()
		{
			var client = CurrentClient;
			var services = DbSession.Query<Service>().Where(s => s.IsActivableFromWeb);
			var blockAccountService = services.FirstOrDefault(i => i is BlockAccountService);
			var deferredPayment = services.FirstOrDefault(i => i is DeferredPayment);
			var inforoomServices = new List<Service> { blockAccountService, deferredPayment };

			ViewBag.Client = client;
			//@todo Убрать исключения для статического IP, когда будет внедрено ручное включение сервиса
			ViewBag.ClientServices = client.ClientServices.Where(cs => (cs.Service.Name == "Фиксированный ip-адрес" || cs.Service.IsActivableFromWeb) && cs.IsActivated).ToList();
			ViewBag.AvailableServices = inforoomServices;

			ViewBag.BlockAccountService = blockAccountService;
			ViewBag.DeferredPayment = deferredPayment;
		}

		//@todo убрать этот бред - заменить функцией с return
		private void InitPlans(Client client)
		{
			IList<Plan> plans;
			//если адреса нет, показываем все тарифы
			if (client.PhysicalClient.Address != null) {
				//Если у тарифа нет региона, то он доступен во всех регионах
				plans = GetList<Plan>().Where(p => !p.IsArchived && !p.IsServicePlan && (!p.RegionPlans.Any() || p.RegionPlans.Any(r => r.Region.Id == client.PhysicalClient.Address.House.Street.Region.Id))).ToList();
			}
			else {
				plans = GetList<Plan>().Where(p => !p.IsArchived && !p.IsServicePlan && !p.RegionPlans.Any()).ToList();
			}

			ViewBag.Plans = plans.ToList();
		}
	}
}