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
			return View();
		}

		/// <summary>
		/// Создание счета
		/// </summary>
		[HttpPost]
		public ActionResult PaymentCreate([EntityBinder] BankPayment payment)
		{
			var errors = ValidationRunner.Validate(payment);
			if (errors.Length > 0) {
				return View();
			}
			var newPayment = new Payment()
			{
				Client = payment.Payer,
				Sum = payment.Sum,
				RecievedOn = payment.RegistredOn,
				PaidOn = payment.PayedOn,
				Employee = GetCurrentEmployee(),
				BankPayment = payment
			};
			errors = ValidationRunner.Validate(newPayment);
			if (errors.Length > 0) {
				return View();
			}
			payment.RegisterPayment();
			DbSession.Save(payment);
			DbSession.Save(newPayment);
			SuccessMessage($"Платеж от '{payment.Id}' успешно добавлен!");
			return RedirectToAction("PaymentList"); 
		}

		/// <summary>
		/// Создание счета
		/// </summary>
		public ActionResult PaymentEdit(int id)
		{
			ViewBag.BankPayment = DbSession.Query<BankPayment>().FirstOrDefault(s => s.Id == id);
			ViewBag.RecipientList = DbSession.Query<Recipient>().OrderBy(s => s.Name).ToList();

			return View();
		}

		private void Cancel(Payment payment, string comment, Employee employee)
		{
			if (!string.IsNullOrEmpty(comment)) {
				var message = payment.Cancel(comment, employee);
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
				var emails = str.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
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
		/// Создание счета
		/// </summary>
		[HttpPost]
		public ActionResult PaymentEdit([EntityBinder] BankPayment bankPayment, bool paymentUpdatePayerInn)
		{
			ViewBag.BankPayment = bankPayment;
			ViewBag.RecipientList = DbSession.Query<Recipient>().OrderBy(s => s.Name).ToList();

			var error = ValidationRunner.Validate(bankPayment);
			if (error.Count != 0) {
				ViewBag.SessionToRefresh = DbSession;
				return View();
			}
			error = ValidationRunner.Validate(bankPayment.Payment);
			if (error.Count != 0) {
				ViewBag.SessionToRefresh = DbSession;
				return View();
			}
			bankPayment.UpdateInn();
			DbSession.Save(bankPayment);
			SuccessMessage($"Платеж от '{bankPayment.PayerName}' успешно обновлен");
			return RedirectToAction("PaymentList");
		}

		/// <summary>
		/// Формирование документов
		/// </summary>
		public ActionResult PaymentProcess()
		{
			return View();
		}
	}
}