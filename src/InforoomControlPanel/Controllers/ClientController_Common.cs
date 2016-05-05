using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.UI;
using Common.MySql;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom2.Components;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using InforoomControlPanel.ReportTemplates;
using InternetInterface.Helpers;
using NHibernate.Linq;
using NHibernate.Util;
using Remotion.Linq.Clauses;
using Agent = Inforoom2.Models.Agent;
using Client = Inforoom2.Models.Client;
using ClientService = Inforoom2.Models.ClientService;
using Contact = Inforoom2.Models.Contact;
using House = Inforoom2.Models.House;
using PhysicalClient = Inforoom2.Models.PhysicalClient;
using RequestType = Inforoom2.Models.RequestType;
using ServiceRequest = Inforoom2.Models.ServiceRequest;
using Status = Inforoom2.Models.Status;
using StatusType = Inforoom2.Models.StatusType;
using Street = Inforoom2.Models.Street;
using NHibernate;
using NHibernate.Proxy.DynamicProxy;
using NHibernate.Transform;
using NHibernate.Validator.Engine;
using SceHelper = Inforoom2.Helpers.SceHelper;

namespace InforoomControlPanel.Controllers
{
	public partial class ClientController : ControlPanelController
	{


		/// <summary>
		/// Страница списка клиентов
		/// </summary>
		public ActionResult List(bool openInANewTab = true, bool error = false)
		{
			InforoomModelFilter<Client> pager = null;
			try
			{
				pager = new InforoomModelFilter<Client>(this);
				pager = ClientReports.GetGeneralReport(this, pager, false);
			}
			catch (Exception ex)
			{
				if (!(ex is FormatException))
				{
					throw ex;
				}
				pager = null;
			}
			if (pager == null)
			{
				return RedirectToAction("List", new { @error = true });
			}
			if (error)
			{
				ErrorMessage("Ошибка ввода: неподдерживаемый формат введенных данных.");
			}
			if (!openInANewTab && pager.TotalItems == 1)
			{
				var clientList = pager.GetItems(); 

				return RedirectToAction((clientList.First().PhysicalClient != null ? "InfoPhysical" : "InfoLegal"), "Client",
					new { clientList.First().Id });
			}
			return View("List");
		}

		public ActionResult ActivateService(int clientId, int serviceId, DateTime? startDate, DateTime? endDate)
		{
			var servise = DbSession.Load<Inforoom2.Models.Services.Service>(serviceId);
			var client = DbSession.Load<Client>(clientId);
			var currentDate = DateTime.Now;
			//Валидации даты
			if (startDate.HasValue && endDate.HasValue) {
				var lessThanPast = DateTime.Compare(endDate.Value.Date, SystemTime.Now().Date);
				var lessThanCurrent = DateTime.Compare(endDate.Value.Date, startDate.Value.Date);
				if (lessThanPast != 1 || lessThanCurrent != 1) {
					ErrorMessage("Дата окончания может быть выставлена только для будущего периода");
					return RedirectToAction(client.IsPhysicalClient ? "InfoPhysical" : "InfoLegal", new {client.Id});
				}
			}
			if (endDate.HasValue) {
				var lessThanPast = DateTime.Compare(endDate.Value.Date, SystemTime.Now().Date);
				if (lessThanPast != 1) {
					ErrorMessage("Дата окончания может быть выставлена только для будущего периода");
					return RedirectToAction(client.IsPhysicalClient ? "InfoPhysical" : "InfoLegal", new {client.Id});
				}
			}
			if (servise.InterfaceControl) {
				var clientService = new ClientService
				{
					Client = client,
					Service = servise,
					BeginDate =
						startDate == null
							? currentDate
							: new DateTime(startDate.Value.Year,
								startDate.Value.Month,
								startDate.Value.Day, currentDate.Hour,
								currentDate.Minute,
								currentDate.Second),
					EndDate = endDate == null
						? endDate
						: new DateTime(endDate.Value.Year,
							endDate.Value.Month,
							endDate.Value.Day,
							currentDate.Hour,
							currentDate.Minute,
							currentDate.Second),
					Employee = GetCurrentEmployee()
				};
				client.ClientServices.Add(clientService);
				try {
					bool activationResult = clientService.TryActivate(DbSession);
					if (activationResult) {
						var message = string.Format("Услуга \"{0}\" активирована на период с {1} по {2}", clientService.Service.Name,
							clientService.BeginDate != null
								? clientService.BeginDate.Value.ToShortDateString()
								: DateTime.Now.ToShortDateString(),
							clientService.EndDate != null
								? clientService.EndDate.Value.ToShortDateString()
								: string.Empty);
						client.Appeals.Add(new Appeal(message, client, AppealType.System, GetCurrentEmployee()));
						SuccessMessage(message);
					}
					else {
						ErrorMessage(String.Format("Невозможно активировать услугу \"{0}\"", clientService.Service.Name));
					}
					if (client.IsNeedRecofiguration)
						SceHelper.UpdatePackageId(DbSession, client);
				}
				catch (Exception e) {
					ErrorMessage(e.Message);
				}
				DbSession.Update(client);
			}
			return RedirectToAction(client.IsPhysicalClient ? "InfoPhysical" : "InfoLegal", new {client.Id});
		}

		[HttpPost]
		public ActionResult DiactivateService(int clientId, int serviceId)
		{
			var client = DbSession.Load<Client>(clientId);
			var clientService = client.ClientServices.FirstOrDefault(c => c.Service.Id == serviceId && c.IsActivated);
			if (clientService != null) {
				bool activationResult = clientService.TryDeactivate(DbSession);
				if (activationResult) {
					var message = string.Format("Услуга \"{0}\" успешно деактивирована.", clientService.Service.Name);
					client.Appeals.Add(new Appeal(message, client, AppealType.System, GetCurrentEmployee()));
					SuccessMessage(message);
				}
				else {
					ErrorMessage(String.Format("Невозможно деактивировать услугу \"{0}\"", clientService.Service.Name));
				}
				if (client.IsNeedRecofiguration)
					SceHelper.UpdatePackageId(DbSession, client);
				DbSession.Update(client);
			}
			return RedirectToAction(client.IsPhysicalClient ? "InfoPhysical" : "InfoLegal", new {client.Id});
		}


		/// <summary>
		/// Страница списка клиентов
		/// </summary>
		public ActionResult Appeals(bool openInANewTab = true)
		{
			var pager = new InforoomModelFilter<Appeal>(this);
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.Date")) &&
			    string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.Date"))) {
				pager.ParamDelete("filter.GreaterOrEqueal.Date");
				pager.ParamDelete("filter.LowerOrEqual.Date");
				pager.ParamSet("filter.GreaterOrEqueal.Date", SystemTime.Now().Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.Date", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
			}
			var criteria = pager.GetCriteria();
			ViewBag.Pager = pager;
			return View();
		}

		/// <summary>
		/// Страница списка клиентов
		/// </summary>
		public ActionResult ListOnline(bool openInANewTab = true)
		{
			var packageSpeedList = DbSession.Query<PackageSpeed>().ToList();

			var leaseWithoutEndPointList = DbSession.Query<Lease>().Where(s => s.Endpoint == null).ToList();
			var pager = new InforoomModelFilter<Lease>(this);

			var ipLease = pager.GetParam("LeaseListByIp") ?? "";
			pager.ParamDelete("LeaseListByIp");
			var splitAndFirst = ipLease.Split(',').FirstOrDefault();
			ipLease = splitAndFirst ?? "";
			ViewBag.LeaseListByIp = splitAndFirst ?? "";
			if (!string.IsNullOrEmpty(ipLease)) {
				IPAddress ipaddressModel = null;

				if (ipLease.Count(s => s == '.') < 3 || ipLease.Last() == '.') {
					ipLease = ipLease.Trim();
					int p1 = -1, p2 = -1, p3 = -1, p4 = -1;
					var currentIp = "";
					var currentIpNext = "";
					var splitedIp = ipLease.Split('.');
					if (splitedIp.Length > 3 && int.TryParse(splitedIp[3], out p4)) {
						currentIp = "." + p4;
						currentIpNext = "." + p4;
					}
					else {
						currentIp = ".0";
						currentIpNext = ".0";
					}
					if (splitedIp.Length > 2 && int.TryParse(splitedIp[2], out p3)) {
						currentIp = "." + p3 + currentIp;
						currentIpNext = "." + (p4 == -1 || p4 == 0 ? p3 + 1 : p3) + currentIpNext;
					}
					else {
						currentIp = ".0" + currentIp;
						currentIpNext = ".0" + currentIpNext;
					}
					if (splitedIp.Length > 1 && int.TryParse(splitedIp[1], out p2)) {
						currentIp = "." + p2 + currentIp;
						currentIpNext = "." + (p3 == -1 || p3 == 0 ? p2 + 1 : p2) + currentIpNext;
					}
					else {
						currentIp = ".0" + currentIp;
						currentIpNext = ".0" + currentIpNext;
					}
					if (splitedIp.Length > 0 && int.TryParse(splitedIp[0], out p1)) {
						currentIp = p1 + currentIp;
						currentIpNext = (p2 == -1 || p2 == 0 ? p1 + 1 : p1) + currentIpNext;
					}
					else {
						currentIp = "0" + currentIp;
						currentIpNext = "1" + currentIpNext;
					}

					IPAddress ipaddressModelNext = null;


					IPAddress.TryParse(currentIp, out ipaddressModel);
					IPAddress.TryParse(currentIpNext, out ipaddressModelNext);
					if (ipaddressModel != null) {
						pager.ParamDelete("filter.Greater.Endpoint.LeaseList.First().Ip");
						pager.ParamSet("filter.Greater.Endpoint.LeaseList.First().Ip", ipaddressModel.ToString());
						if (ipaddressModelNext != null) {
							pager.ParamDelete("filter.Lower.Endpoint.LeaseList.First().Ip");
							pager.ParamSet("filter.Lower.Endpoint.LeaseList.First().Ip", ipaddressModelNext.ToString());
						}
						pager.ParamSet("LeaseListByIp", ipLease);
					}
				}
				else {
					IPAddress.TryParse(ipLease, out ipaddressModel);
					if (ipaddressModel != null) {
						pager.ParamSet("filter.Equal.Endpoint.LeaseList.First().Ip", ipaddressModel.ToString());
						pager.ParamSet("LeaseListByIp", ipLease);
					}
				}
			}

			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			var cr = pager.GetCriteria();
			ViewBag.Pager = pager;
			ViewBag.LeaseWithoutEndPointList = leaseWithoutEndPointList;
			ViewBag.PackageSpeedList = packageSpeedList;
			return View();
		}

		/// <summary>
		/// Страница списка клиентов
		/// </summary>
		public ActionResult LeasesLog(int Id = 0)
		{
			var pager = new FilterReport<Internetsessionslog>(this);
			if (Id == 0 && string.IsNullOrEmpty(pager.GetParam("Id")) == false) {
				int.TryParse(pager.GetParam("Id"), out Id);
			}
			if (Id == 0) {
				return RedirectToAction("List");
			}
			var client = DbSession.Query<Client>().FirstOrDefault(s => s.Id == Id);
			if (client == null) {
				return RedirectToAction("List");
			}

			var result = LeaseReport.GetGeneralReport(this, pager, DbSession, client);
			if (result == null) {
				return RedirectToAction("List");
			}

			ViewBag.Result = result;
			ViewBag.Pager = pager;
			ViewBag.Client = client;

			return View();
		}

		[HttpPost]
		public ActionResult AddPayment([EntityBinder] Client client, string sum = "", string comment = "",
			bool isBonus = false, string subViewName = "")
		{
			decimal realSum = 0m;
			Decimal.TryParse(sum.ToString().Replace(".", ","), out realSum);
			if (realSum > 0) {
				if (client.LegalClient == null) {
					realSum = Decimal.Round(realSum, 2);
					var payment = new Payment()
					{
						Client = client,
						Comment = comment,
						Employee = GetCurrentEmployee(),
						PaidOn = SystemTime.Now(),
						RecievedOn = SystemTime.Now(),
						Virtual = isBonus,
						Sum = realSum,
						BillingAccount = false
					};
					client.Payments.Add(payment);
					DbSession.Save(payment);
					DbSession.Save(client);
					SuccessMessage("Платеж успешно добавлен и ожидает обработки");
				}
				else {
					ErrorMessage("Юридические лица не могут оплачивать наличностью");
				}
			}
			else {
				ErrorMessage("Платеж не был добавлен: данные введены неверно");
			}

			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}

		[HttpPost]
		public ActionResult MovePayment([EntityBinder] Client client, int clientReceiverId = 0, int paymentId = 0,
			string comment = "", string subViewName = "")
		{
			var clientReceiver = DbSession.Query<Client>().FirstOrDefault(s => s.Id == clientReceiverId);

			if (clientReceiver == null && string.IsNullOrEmpty(comment)) {
				ErrorMessage("Платеж не был переведен: указанный лицевой счет не существует, причина перевода не указана");
				return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
					new {@Id = client.Id, @subViewName = subViewName});
			}
			if (clientReceiver == null) {
				ErrorMessage("Платеж не был переведен: указанный лицевой счет не существует");
				return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
					new {@Id = client.Id, @subViewName = subViewName});
			}
			if (string.IsNullOrEmpty(comment)) {
				ErrorMessage("Платеж не был переведен: не указана причина перевода");
				return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
					new {@Id = client.Id, @subViewName = subViewName});
			}

			decimal paymentSum = -1;
			if (paymentId != 0) {
				var payment = client.Payments.FirstOrDefault(s => s.Id == paymentId);
				if (!payment.BillingAccount) {
					ErrorMessage($"Платеж {paymentId} не был переведен: платеж ожидает обработки");
					return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
						new {@Id = client.Id, @subViewName = subViewName});
				}
				paymentSum = payment.Sum;
				var appeal = payment.Cancel(comment, GetCurrentEmployee());
				appeal.Message = appeal.Message.ReplaceSharpWithRedmine();
				client.Appeals.Add(appeal);

				payment.Client.Payments.Remove(payment);
				payment.Client = clientReceiver;
				payment.BillingAccount = false;
				clientReceiver.Payments.Add(payment);

				var msgTextFormat =
					"Платеж №{4} клиента №<a href='{5}'>{1}</a> на сумму {0} руб.  был перемещен клиенту №<a href='{6}'>{2}</a>.<br/>Комментарий: {3} ";
				var msgTextA = String.Format(msgTextFormat + "<br/>Баланс: {7}. ", payment.Sum.ToString("0.00"),
					client.Id, clientReceiver.Id, comment, payment.Id,
					ConfigHelper.GetParam("adminPanelNew") +
					Url.Action(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal", new {@Id = client.Id}),
					ConfigHelper.GetParam("adminPanelNew") +
					Url.Action(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal", new {@Id = clientReceiver.Id}),
					client.Balance.ToString("0.00"));
				var msgTextB = String.Format(msgTextFormat, payment.Sum.ToString("0.00"), client.Id, clientReceiver.Id, comment,
					payment.Id,
					ConfigHelper.GetParam("adminPanelNew") +
					Url.Action(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal", new {@Id = client.Id}),
					ConfigHelper.GetParam("adminPanelNew") +
					Url.Action(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal", new {@Id = clientReceiver.Id}));
				var clientAppeal = new Appeal(msgTextA, client, AppealType.System)
				{
					Employee = GetCurrentEmployee(),
					inforoom2 = true
				};
				var clientReceiverAppeal = new Appeal(msgTextB, clientReceiver, AppealType.System)
				{
					Employee = GetCurrentEmployee(),
					inforoom2 = true
				};

				DbSession.Save(payment);
				client.Appeals.Add(clientAppeal);
				clientReceiver.Appeals.Add(clientReceiverAppeal);


				DbSession.Save(client);
				DbSession.Save(clientReceiver);


				var appealText = string.Format(@"
Переведен платеж №{0}
От клиента: №{1}
Клиенту: №{2}
Сумма: {3}
Оператор: {4}
Комментарий: {5}
", paymentId, client.Name + " (" + client.Id + ") ", clientReceiver.Name + " (" + clientReceiver.Id + ") ",
					paymentSum.ToString("0.00"), GetCurrentEmployee().Name, comment);


				string emails = "InternetBilling@analit.net";
#if DEBUG
#else
						EmailSender.SendEmail(emails, "Переведен платеж", appealText);
#endif


				SuccessMessage("Платеж успешно переведен и ожидает обработки");
			}
			else {
				ErrorMessage("Платеж не был переведен: платежа с данным номером в базе нет");
			}

			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}

		[HttpPost]
		public ActionResult RemovePayment([EntityBinder] Client client, int paymentId = 0, string comment = "",
			string subViewName = "")
		{
			if (string.IsNullOrEmpty(comment)) {
				ErrorMessage($"Платеж {paymentId} не был отменен: не указана причина отмены");
				return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
					new {@Id = client.Id, @subViewName = subViewName});
			}
			decimal paymentSum = -1;
			if (paymentId != 0) {
				var payment = client.Payments.FirstOrDefault(s => s.Id == paymentId);
				if (!payment.BillingAccount) {
					ErrorMessage($"Платеж {paymentId} не был отменен: платеж ожидает обработки");
					return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
						new {@Id = client.Id, @subViewName = subViewName});
				}
				paymentSum = payment.Sum;
				var appeal = payment.Cancel(comment, GetCurrentEmployee());
				appeal.Message = appeal.Message.ReplaceSharpWithRedmine();
				client.Appeals.Add(appeal);
				client.Payments.Remove(payment);
				DbSession.Save(client);
				DbSession.Delete(payment);
			}

			if (paymentSum != -1) {
				SuccessMessage($"Платеж {paymentId} успешно отменен!");

				var str = ConfigHelper.GetParam("PaymentNotificationMail");
				if (str == null)
					throw new Exception("Параметр приложения PaymentNotificationMail должен быть задан в config");
				var appealText = string.Format(@"
Отменен платеж №{0}
Клиент: №{1} - {2}
Сумма: {3:C}
Оператор: {4}
Комментарий: {5}
", paymentId, client.Id, client.Name, paymentSum, GetCurrentEmployee().Name, comment);


				var emails = str.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
#if DEBUG
#else
						EmailSender.SendEmail(emails, "Уведомление об отмене платежа", appealText);
#endif
			}
			else {
				ErrorMessage($"Платеж {paymentId} не был удален: платежа по данному номеру не существует");
			}
			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}

		[HttpPost]
		public ActionResult AddWriteOff([EntityBinder] Client client, string sum = "", string comment = "",
			string subViewName = "")
		{
			decimal realSum = 0m;
			Decimal.TryParse(sum.ToString().Replace(".", ","), out realSum);
			if (realSum > 0 && comment != "") {
				realSum = Decimal.Round(realSum, 2);
				var writeOff = new UserWriteOff()
				{
					Comment = comment,
					Sum = realSum,
					Date = SystemTime.Now(),
					Client = client,
					Employee = GetCurrentEmployee(),
					IsProcessedByBilling = false
				};
				client.UserWriteOffs.Add(writeOff);
				DbSession.Save(client);
				SuccessMessage("Списание успешно добавлено и ожидает обработки");
			}
			else {
				ErrorMessage("Списание не было добавлено: данные введены неверно");
			}

			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}

		[HttpPost]
		public ActionResult DeleteWriteOff([EntityBinder] Client client, int writeOffId = 0, int user = 0, string comment = "",
			string subViewName = "")
		{
			decimal writeOffSum = -1;
			if (string.IsNullOrEmpty(comment)) {
				ErrorMessage($"Списание {writeOffId} не было удалено: не указана причина отмены");
				return RedirectToAction("InfoPhysical", new {@Id = client.Id, @subViewName = subViewName});
			}
			if (user == 1) {
				var writeOff = client.UserWriteOffs.FirstOrDefault(s => s.Id == writeOffId);
				if (!writeOff.IsProcessedByBilling) {
					ErrorMessage($"Списание {writeOffId} не было удалено: списание ожидает обработки");
					return RedirectToAction("InfoPhysical", new {@Id = client.Id, @subViewName = subViewName});
				}
				writeOffSum = writeOff.Sum;
				var appeal = writeOff.Cancel(GetCurrentEmployee(), comment);
				appeal.Message = appeal.Message.ReplaceSharpWithRedmine();
				client.Appeals.Add(appeal);
				client.UserWriteOffs.Remove(writeOff);
				DbSession.Save(client);
			}
			else {
				var writeOff = client.WriteOffs.FirstOrDefault(s => s.Id == writeOffId);
				writeOffSum = writeOff.WriteOffSum;
				var appeal = writeOff.Cancel(GetCurrentEmployee(), comment);
				client.Appeals.Add(appeal);
				client.WriteOffs.Remove(writeOff);
				DbSession.Save(client);
			}

			if (writeOffSum != -1) {
				SuccessMessage($"Списание {writeOffId} успешно удалено!");

				var str = ConfigHelper.GetParam("WriteOffNotificationMail");
				if (str == null)
					throw new Exception("Параметр приложения WriteOffNotificationMail должен быть задан в config");
				var appealText = string.Format(@"
Отменено списание №{0}
Клиент: №{1} - {2}
Сумма: {3}
Оператор: {4}
Комментарий: {5}
", writeOffId, client.Id, client.Name, writeOffSum, GetCurrentEmployee().Name, comment);

				var emails = str.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
#if DEBUG
#else
						EmailSender.SendEmail(emails, "Уведомление об удалении списания", appealText);
#endif
			}
			else {
				ErrorMessage($"Списание {writeOffId} не было удалено: списания по данному номеру не существует");
			}

			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}


		[HttpPost]
		public ActionResult RemoveEndpoint([EntityBinder] Client client, int endpointId,
			string subViewName = "")
		{
			var endPoint = client.Endpoints.FirstOrDefault(s => s.Id == endpointId);
			if (endPoint != null) {
				//TODO: важно! SQL запрос необходим для удаления элемента (прежний вариант с отчисткой списка удалял клиентов у endpoint(ов))
				if (!client.RemoveEndpoint(endPoint, DbSession))
					ErrorMessage("Последняя точка подключения не может быть удалена!");
			}
			return RedirectToAction(client.PhysicalClient != null ? "InfoPhysical" : "InfoLegal",
				new {@Id = client.Id, @subViewName = subViewName});
		}


		///| -----------------------------------------------|Агенты (не ясно нужны или нет)|------------------------------------------------->>
		public ActionResult AgentList()
		{
			var Agent = DbSession.QueryOver<Agent>().List();
			Agent = Agent.OrderByDescending(s => s.Active).ThenBy(s => s.Name).ToList();
			var employee = DbSession.QueryOver<Employee>().List();
			ViewBag.AgentList = Agent;
			ViewBag.AgentMan = new Agent();
			return View("AgentList");
		}

		public ActionResult AgentAdd([EntityBinder] Agent agent)
		{
			var existedAgent = DbSession.Query<Agent>()
				.FirstOrDefault(s => s.Name.ToLower().Replace(" ", "") == agent.Name.ToLower().Replace(" ", ""));
			if (existedAgent == null) {
				var errors = ValidationRunner.ValidateDeep(agent);
				if (errors.Length == 0) {
					DbSession.Save(agent);
					SuccessMessage("Агент успешно добавлен");
				}
				else {
					ErrorMessage(errors[0].Message);
				}
			}
			else {
				ErrorMessage("Агент с подобным ФИО уже существует!");
			}
			return RedirectToAction("AgentList");
		}

		public ActionResult AgentStatusChange(int id)
		{
			var Agent = DbSession.Query<Agent>().FirstOrDefault(s => s.Id == id);
			if (Agent != null) {
				Agent.Active = !Agent.Active;
				DbSession.Update(Agent);
			}
			return RedirectToAction("AgentList");
		}

		public ActionResult AgentDelete(int id)
		{
			var Agent = DbSession.Query<Agent>().FirstOrDefault(s => s.Id == id);
			if (Agent != null) {
				DbSession.Delete(Agent);
			}
			return RedirectToAction("AgentList");
		}

		//<-----------------------------------------------------------------------------------------------------------------------------------||
	}
}