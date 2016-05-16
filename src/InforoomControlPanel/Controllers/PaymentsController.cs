using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Helpers;
using Inforoom2.Models;
using InforoomControlPanel.ReportTemplates;
using InforoomControlPanel.Services;
using NHibernate.Linq;
using NHibernate.Mapping;
using NHibernate.Util;
using Remotion.Linq.Clauses;
using Client = Inforoom2.Models.Client;
using PackageSpeed = Inforoom2.Models.PackageSpeed;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления баннерами
	/// </summary>
	public class PaymentsController : ControlPanelController
	{
		//todo: вывод после анализа перенесенного функ. : нужно правильно связать Payment и BankPayment, т.к. при изменении их связи теряются 

		/// <summary>
		/// Список платежей
		/// </summary>
		public ActionResult PaymentByEmployees()
		{
			var pager = new InforoomModelFilter<Client>(this);

			var employeesListId = DbSession.Query<Client>().Where(s => s.WhoRegistered != null).Select(s => s.WhoRegistered.Id).Distinct().ToList();
			var employeesList = DbSession.Query<Employee>().ToList();
			employeesList = employeesList.Where(s => employeesListId.Any(d => d == s.Id)).OrderBy(s => s.Name).ToList();
			//var dateA = DateTime.Parse(pager.GetParam("filter.GreaterOrEqueal.PaidOn"));
			int currentEmployeeId = 0;
			if (!string.IsNullOrEmpty(pager.GetParam("filter.Equal.WhoRegistered.Id"))) {
				var first = pager.GetParam("filter.Equal.WhoRegistered.Id").Split(',');
				int.TryParse(first.Length > 0 ? first[0] : "", out currentEmployeeId);
			}
			pager.ParamDelete("filter.Equal.WhoRegistered.Id");
			currentEmployeeId = currentEmployeeId != 0 ? currentEmployeeId : 0;
			if (currentEmployeeId != 0) {
				pager.ParamSet("filter.Equal.WhoRegistered.Id", currentEmployeeId.ToString());
			}
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("WhoRegistered.Name", OrderingDirection.Asc);
			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.CreationDate")) &&
			    string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.CreationDate"))) {
				pager.ParamDelete("filter.GreaterOrEqueal.CreationDate");
				pager.ParamDelete("filter.LowerOrEqual.CreationDate");
				pager.ParamSet("filter.GreaterOrEqueal.CreationDate", SystemTime.Now().Date.FirstDayOfMonth().ToString("01.01.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.CreationDate", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
			}
			var criteria = pager.GetCriteria();
			ViewBag.TotalSum = 0; //pager.TotalSumByFieldName("Sum");

			ViewBag.EmployeeCurrent = currentEmployeeId;
			ViewBag.EmployeesList = employeesList;
			ViewBag.pager = pager;
			return View();
		}

		/// <summary>
		/// Список платежей
		/// </summary>
		public ActionResult PaymentList()
		{
			var pager = new InforoomModelFilter<BankPayment>(this);

			//var dateA = DateTime.Parse(pager.GetParam("filter.GreaterOrEqueal.PaidOn"));

			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("PayedOn", OrderingDirection.Desc);

			var selectedMonth = pager.GetParam("selectedMonth");
			var selectedYear = pager.GetParam("selectedYear");
			pager.ParamDelete("selectedYear");
			pager.ParamDelete("selectedMonth");
			DateTime selectedDate = DateTime.MinValue;
			if (!string.IsNullOrEmpty(selectedMonth) && !string.IsNullOrEmpty(selectedYear)) {
				DateTime.TryParse($"01.{selectedMonth}.{selectedYear}", out selectedDate);
				if (selectedDate != DateTime.MinValue) {
					pager.ParamDelete("filter.GreaterOrEqueal.PayedOn");
					pager.ParamDelete("filter.LowerOrEqual.PayedOn");
					pager.ParamSet("filter.GreaterOrEqueal.PayedOn", selectedDate.Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
					pager.ParamSet("filter.LowerOrEqual.PayedOn", selectedDate.Date.LastDayOfMonth().ToString("dd.MM.yyyy"));
				}
			}

			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.PayedOn")) &&
			    string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.PayedOn"))) {
				pager.ParamDelete("filter.GreaterOrEqueal.PayedOn");
				pager.ParamDelete("filter.LowerOrEqual.PayedOn");
				pager.ParamSet("filter.GreaterOrEqueal.PayedOn", SystemTime.Now().Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.PayedOn", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
			}

			var dateB = DateTime.Parse(pager.GetParam("filter.LowerOrEqual.PayedOn")).AddDays(1).AddSeconds(-1);

			var criteria = pager.GetCriteria();
			ViewBag.TotalSum = pager.TotalSumByFieldName("Sum");

			var firstYear = DbSession.Query<BankPayment>().OrderBy(s => s.Id).FirstOrDefault();
			var lastYear = DbSession.Query<BankPayment>().OrderByDescending(s => s.Id).FirstOrDefault();

			ViewBag.FirstYear = firstYear?.PayedOn.Year ?? 0;
			ViewBag.FirstMonth = firstYear?.PayedOn.Month ?? 0;
			ViewBag.LastYear = lastYear?.PayedOn.Year ?? 0;
			ViewBag.LastMonth = lastYear?.PayedOn.Month ?? 0;
			ViewBag.CurrentYear = dateB.Year;
			ViewBag.CurrentMonth = dateB.Month;
			ViewBag.pager = pager;
			return View();
		}

		/// <summary>
		/// Создание счета
		/// </summary> 
		public ActionResult PaymentCreate()
		{
			ViewBag.RecipientList = DbSession.Query<Recipient>().OrderBy(s => s.Name).ToList();
			return View();
		}

		/// <summary>
		/// Создание счета
		/// </summary>
		[HttpPost]
		public ActionResult PaymentCreate([EntityBinder] BankPayment bankPayment)
		{
			var errors = ValidationRunner.Validate(bankPayment);
			if (errors.Length > 0) {
				ErrorMessage("Произошла ошибка при добавлении платежа, платеж не был добавлен.");
				ViewBag.RecipientList = DbSession.Query<Recipient>().OrderBy(s => s.Name).ToList();
				ViewBag.BankPayment = bankPayment;
				PreventSessionUpdate();
				return View();
			}
			bankPayment.RegisterPayment();
			var newPayment = new Payment() {
				Client = bankPayment.Payer,
				Sum = bankPayment.Sum,
				RecievedOn = bankPayment.RegistredOn,
				PaidOn = bankPayment.PayedOn,
				Employee = GetCurrentEmployee(),
				BankPayment = bankPayment
			};
			bankPayment.Payment = newPayment;
			errors = ValidationRunner.Validate(newPayment);
			if (errors.Length > 0) {
				ErrorMessage("Произошла ошибка при добавлении платежа, платеж не был добавлен.");
				ViewBag.RecipientList = DbSession.Query<Recipient>().OrderBy(s => s.Name).ToList();
				ViewBag.BankPayment = bankPayment;
				PreventSessionUpdate();
				return View();
			}
			if (bankPayment.Recipient != bankPayment.Payer.Recipient) {
				ErrorMessage(
					$"Произошла ошибка при добавлении платежа, получателем платежей данного клиента может быть только {bankPayment.Payer.Recipient.Name}.");
				ViewBag.RecipientList = DbSession.Query<Recipient>().OrderBy(s => s.Name).ToList();
				ViewBag.BankPayment = bankPayment;
				PreventSessionUpdate();
				return View();
			}
			DbSession.Save(bankPayment);
			DbSession.Save(newPayment);
			SuccessMessage($"Платеж от '{bankPayment.Id}' успешно добавлен!");
			return RedirectToAction("PaymentList");
		}

		/// <summary>
		/// Информация по счету счета
		/// </summary>
		public ActionResult PaymentInfo(int id)
		{
			ViewBag.BankPayment = DbSession.Query<BankPayment>().FirstOrDefault(s => s.Id == id);
			return View();
		}

		private void Cancel(Payment payment, string comment, Employee employee)
		{
			if (!string.IsNullOrEmpty(comment)) {
				var message = payment.Cancel(comment, employee);
				message.Message = message.Message.ReplaceSharpWithRedmine();
				DbSession.Delete(payment);
				DbSession.Save(message);
				SuccessMessage($"Платеж {payment.Id} успешно отменен!");
				var str = ConfigHelper.GetParam("PaymentNotificationMail");
				if (str == null)
					throw new Exception("Параметр приложения PaymentNotificationMail должен быть задан в config");
				var appealText = string.Format(@"
Отменен платеж №{0}
Клиент: №{1} - {2}
Сумма: {3:C}
Оператор: {4}
Комментарий: {5}
", payment.Id, payment.Client.Id, payment.Client.Name, payment.Sum, GetCurrentEmployee().Name, comment);
				var emails = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
#if DEBUG
#else
						EmailSender.SendEmail(emails, "Уведомление об отмене платежа", appealText);
#endif
			}
		}

		/// <summary>
		/// Создание счета
		/// </summary>
		public ActionResult PaymentRemove(int id)
		{
			var paymentToDelete = DbSession.Query<BankPayment>().FirstOrDefault(s => s.Id == id);
			if (paymentToDelete == null) {
				SuccessMessage($"Платежа с идентификатором '{id}' не существует.");
			}
			if (paymentToDelete.Payment != null) {
				Cancel(paymentToDelete.Payment,
					string.Format("Был удален банковский платеж от {0} на сумму {1}. Комментарий: {2}",
						paymentToDelete.PayedOn.ToShortDateString(), paymentToDelete.Sum, paymentToDelete.Comment), GetCurrentEmployee());
			}
			DbSession.Delete(paymentToDelete);
			SuccessMessage($"Платеж от '{paymentToDelete.PayerName}' успешно удален");
			return RedirectToAction("PaymentList");
		}

		/// <summary>
		/// Формирование документов
		/// </summary>
		[HttpGet]
		public ActionResult PaymentProcess()
		{
			var payments = TempPayments();
			if (payments != null) {
				payments.Each(p => ValidationRunner.Validate(p));
			}
			ViewBag.Payments = payments;
			return View();
		}


		/// <summary>
		/// Формирование документов
		/// </summary>
		[HttpPost]
		public ActionResult PaymentProcess(HttpPostedFileBase uploadedFile)
		{
			var file = uploadedFile;
			if (file == null || file.ContentLength == 0) {
				ErrorMessage("Нужно выбрать файл для загрузки");
				return View();
			}
			var payments = BankPaymentsXmlParser.Parse(DbSession, file.FileName, file.InputStream);
			if (payments.All(p => ValidationRunner.Validate(p).Length == 0)) {
				Session["payments"] = payments;
				ViewBag.Payments = payments;
			}

			else {
				//var errors = payments.Select(p =>
				//{
				//	IsValid(p);
				//	var summary = Validator.GetErrorSummary(p);
				//	if (summary != null && summary.HasError) {
				//		return new {Client = p.PayerClient, Errors = summary.ErrorMessages.ToList()};
				//	}
				//	return null;
				//}).Where(e => e != null).ToList();
				//PropertyBag["errors"] = errors;
				//return;
			}
			return View();
		}

		public ActionResult EditTemp(int id)
		{
			BankPayment payment = FindTempPayment(id);
			if (payment == null) {
				ErrorMessage("Время сессии истекло. Загрузите выписку повторно.");
				return RedirectToAction("PaymentProcess");
			}
			ViewBag.BankPayment = payment;
			ViewBag.RecipientList = DbSession.Query<Recipient>().OrderBy(r => r.Name).ToList();
			ViewBag.EditPaymentWithoutPayer = payment.Payer == null;
			return View("PaymentEdit");
		}

		[HttpPost]
		public ActionResult EditTemp(BankPayment bankPayment)
		{
			var newPayer = bankPayment.Payer != null
				? DbSession.Query<Client>().FirstOrDefault(s => s.Id == bankPayment.Payer.Id)
				: null;
			var payment = FindTempPayment(bankPayment.Id);
			if (payment == null) {
				ErrorMessage("Время сессии истекло. Загрузите выписку повторно.");
				return RedirectToAction("PaymentProcess");
			}

			payment.Sum = bankPayment.Sum;
			payment.Payer = newPayer;
			payment.DocumentNumber = bankPayment.DocumentNumber;
			payment.PayedOn = bankPayment.PayedOn;
			payment.OperatorComment = bankPayment.OperatorComment;
			payment.UpdatePayerInn = bankPayment.UpdatePayerInn;
			ViewBag.EditPaymentWithoutPayer = payment.Payer == null;
			ViewBag.BankPayment = payment;
			ViewBag.RecipientList = DbSession.Query<Recipient>().OrderBy(r => r.Name).ToList();
			var errors = ValidationRunner.Validate(payment);
			if (errors.Length == 0) {
				payment.UpdateInn();
				SuccessMessage("Платеж обновлен");
				return RedirectToAction("PaymentProcess");
			}
			return View("PaymentEdit");
		}

		public ActionResult SavePayments()
		{
			var payments = TempPayments();
			if (payments == null) {
				ErrorMessage("Время сессии истекло. Загрузите выписку повторно.");
				return null;
			}

			foreach (var payment in payments.ToList()) {
				//если зайти в два платежа и отредактировать их
				//то получим двух плательщиков из разных сессий
				//правим это
				if (payment.Payer != null)
					payment.Payer = DbSession.Query<Client>().FirstOrDefault(p => p.Id == payment.Payer.Id);
				var errors = ValidationRunner.Validate(payment);
				if (errors.Length == 0) {
					payment.RegisterPayment();
					DbSession.Save(payment);
					if (payment.Payer != null)
						DbSession.Save(new Payment {
							Client = payment.Payer,
							Sum = payment.Sum,
							RecievedOn = payment.RegistredOn,
							PaidOn = payment.PayedOn,
							Employee = GetCurrentEmployee(),
							BankPayment = payment
						});
				}
				else {
					DbSession.Evict(payment);
				}
			}

			Session["payments"] = null;

			return RedirectToAction("PaymentList",
				new Dictionary<string, string> {
					{ "mfilter.filter.GreaterOrEqueal.PayedOn", payments.Min(p => p.PayedOn).ToShortDateString() },
					{ "mfilter.filter.LowerOrEqual.PayedOn", payments.Max(p => p.PayedOn).ToShortDateString() }
				});
		}


		public ActionResult CancelPayments()
		{
			Session["payments"] = null;
			return RedirectToAction("PaymentProcess");
		}

		private BankPayment FindTempPayment(int id)
		{
			var tempList = TempPayments();
			if (tempList == null) {
				return null;
			}
			return tempList.First(p => p.GetHashCode() == id);
		}

		private List<BankPayment> TempPayments()
		{
			return (List<BankPayment>)Session["payments"];
		}

		public ActionResult DeleteTemp(int id)
		{
			var payment = FindTempPayment(id);
			if (payment == null) {
				ErrorMessage("Время сессии истекло. Загрузите выписку повторно.");
				return RedirectToAction("PaymentProcess");
			}

			TempPayments().Remove(payment);
			return RedirectToAction("PaymentProcess");
		}


		/////////////////////////////////////////Редактирование платежей не было нормально реализовано в старой админке 
		/// (если бы этим функционалом пользовались возникали бы ошибки - вывод данный функционал не востребован - не переношу)
		/// <summary>
		/// Редактирование счета
		/// </summary>
		[HttpGet]
		public ActionResult PaymentEdit(int id)
		{
			ViewBag.BankPayment = DbSession.Query<BankPayment>().FirstOrDefault(s => s.Id == id);
			ViewBag.RecipientList = DbSession.Query<Recipient>().OrderBy(s => s.Name).ToList();

			return View();
		}

		/// <summary>
		/// Редактирование счета
		/// </summary>
		[HttpPost]
		public ActionResult PaymentEdit(int oldPaymentId, BankPayment bankPayment)
		{
			var oldBankPayment = DbSession.Query<BankPayment>().FirstOrDefault(s => s.Id == oldPaymentId);

			if (oldBankPayment == null) {
				return RedirectToAction("PaymentList");
			}
			var newPayer = bankPayment.Payer != null
				? DbSession.Query<Client>().FirstOrDefault(s => s.Id == bankPayment.Payer.Id)
				: null;

			var oldBalance = oldBankPayment.Sum;
			var oldPayer = oldBankPayment.Payer;
			var oldPayment = oldBankPayment.Payment;

			bankPayment.RegisterPayment();

			oldBankPayment.Sum = bankPayment.Sum;
			oldBankPayment.Payer = newPayer;
			oldBankPayment.DocumentNumber = bankPayment.DocumentNumber;
			oldBankPayment.PayedOn = bankPayment.PayedOn;
			oldBankPayment.OperatorComment = bankPayment.OperatorComment;
			oldBankPayment.UpdatePayerInn = bankPayment.UpdatePayerInn;

			if (bankPayment.UpdatePayerInn) {
				oldBankPayment.UpdateInn();
			}

			var newBalance = bankPayment.Sum;
			var newPayerFlag = oldPayer == null && oldBankPayment.Payer != null;

			var errors = ValidationRunner.Validate(oldBankPayment);

			if (errors.Length == 0) {
				if (oldPayer != null && oldBankPayment.Payer == oldPayer) {
					var client = oldBankPayment.Payer;
					if (newBalance - oldBalance < 0 && oldBankPayment.Payer != null) {
						var userWriteOffNew = new UserWriteOff {
							Employee = GetCurrentEmployee(),
							Client = client,
							Sum = oldBalance - newBalance,
							Date = SystemTime.Now(),
							Comment = string.Format("Списание после редактирования банковского платежа (id = {0})", oldBankPayment.Id)
						};
						DbSession.Save(userWriteOffNew);
						client.UserWriteOffs.Add(userWriteOffNew);
						DbSession.Update(client);
					}
					if (newBalance - oldBalance > 0 && oldBankPayment.Payer != null) {
						var paymentNew = new Payment {
							Employee = GetCurrentEmployee(),
							Client = client,
							PaidOn = SystemTime.Now(),
							RecievedOn = SystemTime.Now(),
							Sum = newBalance - oldBalance,
							Comment = string.Format("Зачисление после редактирования банковского платежа (id = {0})", oldBankPayment.Id)
						};
						DbSession.Save(paymentNew);
						client.Payments.Add(paymentNew);
						DbSession.Update(client);
					}
				}
				if (newPayerFlag) {
					var paymentNew = new Payment {
						Employee = GetCurrentEmployee(),
						Client = oldBankPayment.Payer,
						PaidOn = SystemTime.Now(),
						RecievedOn = SystemTime.Now(),
						Sum = oldBankPayment.Sum,
						Comment =
							string.Format("Зачисление после редактирования банковского платежа (id = {0}), назначен плательщик",
								oldBankPayment.Id)
					};
					DbSession.Save(paymentNew);
					oldBankPayment.Payer.Payments.Add(paymentNew);
					DbSession.Update(oldBankPayment.Payer);
				}
				if (oldPayer != null && oldPayer != oldBankPayment.Payer) {
					var paymentNew = new Payment {
						Employee = GetCurrentEmployee(),
						Client = oldBankPayment.Payer,
						PaidOn = SystemTime.Now(),
						RecievedOn = SystemTime.Now(),
						Sum = oldBankPayment.Sum,
						Comment = string.Format("Зачисление после редактирования банковского платежа (id = {0})", oldBankPayment.Id),
						BankPayment = oldBankPayment
					};
					oldBankPayment.Payment = paymentNew;
					oldBankPayment.Payer.Payments.Add(paymentNew);
					DbSession.Save(paymentNew);
					DbSession.Save(oldBankPayment.Payer);
					if (oldPayment != null) {
						Cancel(oldPayment,
							string.Format("Изменение плательщика в банковском платеже {0} с '{1}' на '{2}'", oldBankPayment.Id, oldPayer.Name,
								oldBankPayment.Payer.Name), GetCurrentEmployee());
					}
					else {
						var userWriteOffNew = new UserWriteOff {
							Employee = GetCurrentEmployee(),
							Client = oldPayer,
							Comment =
								string.Format(
									"Списание после смены плательщика, при редактировании банковского платежа №{0} \r\n Клиент стал: {1}",
									oldBankPayment.Id, oldBankPayment.Payer.Id),
							Date = SystemTime.Now(),
							Sum = oldBankPayment.Sum
						};
						var appeal = new Appeal(
							string.Format(
								"После смены плательщика в платеже {0} было создано пользовательское списание, так как не был найден привязанный к банковскому платежу физический платеж для отмены",
								oldBankPayment.Id), oldPayer, AppealType.System);

						oldPayer.UserWriteOffs.Add(userWriteOffNew);
						oldPayer.Appeals.Add(appeal);

						DbSession.Save(userWriteOffNew);
						DbSession.Update(oldPayer);
					}
				}
				DbSession.Save(oldBankPayment);
				SuccessMessage($"Изменения в банковском платеже №{oldBankPayment.Id} сохранены успешно");
			}
			else {
				DbSession.Evict(oldBankPayment);
				PreventSessionUpdate();
			}

			ViewBag.RecipientList = DbSession.Query<Recipient>().OrderBy(s => s.Name).ToList();

			ViewBag.BankPayment = oldBankPayment;
			return View();
		}
	}
}