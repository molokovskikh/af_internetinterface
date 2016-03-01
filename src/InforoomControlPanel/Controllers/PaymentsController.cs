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
			if (!string.IsNullOrEmpty(selectedMonth) && !string.IsNullOrEmpty(selectedYear))
			{
				DateTime.TryParse($"01.{selectedMonth}.{selectedYear}", out selectedDate);
				if (selectedDate != DateTime.MinValue)
				{
					pager.ParamDelete("filter.GreaterOrEqueal.PayedOn");
					pager.ParamDelete("filter.LowerOrEqual.PayedOn");
					pager.ParamSet("filter.GreaterOrEqueal.PayedOn", selectedDate.Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
					pager.ParamSet("filter.LowerOrEqual.PayedOn", selectedDate.Date.LastDayOfMonth().ToString("dd.MM.yyyy"));
				}
			}

			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.PayedOn")) &&
				string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.PayedOn")))
			{
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
		/// Формирование документов
		/// </summary>
		public ActionResult PaymentProcess()
		{
			return View();
		}

		/// <summary>
		/// Отчет по клиентам
		/// </summary>
		public ActionResult Client()
		{
			var pager = new InforoomModelFilter<Client>(this);
			pager = ClientReports.GetGeneralReport(this, pager);
			if (pager == null) {
				return null;
			}
			return View();
		}

		/// <summary>
		/// Отчет по выручке
		/// </summary>
		public ActionResult WriteOffs()
		{
			var pager = new InforoomModelFilter<WriteOff>(this);
			pager = WriteOffsReport.GetGeneralReport(this, pager);
			if (pager == null) {
				return null;
			}
			return View();
		}

		/// <summary>
		/// Отчет по выручке
		/// </summary>
		public ActionResult Payments()
		{
			var pager = new InforoomModelFilter<Payment>(this);
			pager = PaymentsReport.GetGeneralReport(this, pager);
			if (pager == null) {
				return null;
			}
			var dateA = DateTime.Parse(pager.GetParam("filter.GreaterOrEqueal.PaidOn"));
			var dateB = DateTime.Parse(pager.GetParam("filter.LowerOrEqual.PaidOn")).AddDays(1).AddSeconds(-1);
			var clientId = DateTime.Parse(pager.GetParam("filter.GreaterOrEqueal.PaidOn"));
			var clientName = DateTime.Parse(pager.GetParam("filter.LowerOrEqual.PaidOn")).AddDays(1).AddSeconds(-1);

			ViewBag.TotalSum = pager.TotalSumByFieldName("Sum");

			var firstYear = DbSession.Query<Payment>().OrderBy(s => s.Id).FirstOrDefault();
			var lastYear = DbSession.Query<Payment>().OrderByDescending(s => s.Id).FirstOrDefault();

			ViewBag.FirstYear = firstYear?.PaidOn.Year ?? 0;
			ViewBag.FirstMonth = firstYear?.PaidOn.Month ?? 0;
			ViewBag.LastYear = lastYear?.PaidOn.Year ?? 0;
			ViewBag.LastMonth = lastYear?.PaidOn.Month ?? 0;
			ViewBag.CurrentYear = dateB.Year;
			ViewBag.CurrentMonth = dateB.Month;

			return View();
		}
	}
}