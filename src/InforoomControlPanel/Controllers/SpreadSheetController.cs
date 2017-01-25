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
using InforoomControlPanel.Helpers;
using InforoomControlPanel.Models;
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
	public class SpreadSheetController : ControlPanelController
	{
		/// <summary>
		/// Формирование документов
		/// </summary>
		public ActionResult Index()
		{
			return View();
		}

		/// <summary>
		/// Отчет по клиентам
		/// </summary>
		public ActionResult Client()
		{
			var pager = new InforoomModelFilter<Client>(this);
		    var fileRes = ClientReports.GetGeneralReport(this, ref pager, false);
		    if (fileRes != null) {
		        return File(fileRes, "application/ms-excel",
		            $"Отчет по клиентам - {SystemTime.Now().ToString("dd.MM.yyyy HH_mm")}.xls");
		    }
            return View();
		}

		/// <summary>
		/// Отчет по выручке
		/// </summary>
		public ActionResult WriteOffs()
		{
		    var pager = new InforoomModelFilter<WriteOff>(this);
		    var fileRes = WriteOffsReport.GetGeneralReport(this, ref pager, false);
		    if (fileRes != null) {
		        return File(fileRes, "application/ms-excel",
		            $"Отчет по списаниям - {SystemTime.Now().ToString("dd.MM.yyyy HH_mm")}.xls");
		    }
            return View();
		}

		/// <summary>
		/// Отчет по выручке
		/// </summary>
		public ActionResult Payments()
		{
			var pager = new InforoomModelFilter<Payment>(this);
			var currentEmployee = GetCurrentEmployee();
			if (
				(currentEmployee.Permissions.FirstOrDefault(
					s => s.Name == EmployeePermissionViewHelper.FormPermissions.Block_900001.ToString()) != null
					||
					currentEmployee.Roles.Any(
						s => s.Permissions.Any(d => d.Name == EmployeePermissionViewHelper.FormPermissions.Block_900001.ToString()))) ==
					false) {
				pager.ParamDelete("filter.Equal.Employee.Name");
				pager.ParamSet("filter.Equal.Employee.Name", currentEmployee.Name);
			}

			var fileRes = PaymentsReport.GetGeneralReport(this, ref pager, false);
			if (fileRes != null) {
				return File(fileRes, "application/ms-excel",
					$"Отчет по платежам - {SystemTime.Now().ToString("dd.MM.yyyy HH_mm")}.xls");
			}

			var dateB = DateTime.Parse(pager.GetParam("filter.LowerOrEqual.PaidOn")).AddDays(1).AddSeconds(-1);

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


		/// <summary>
		/// Отчет по выручке
		/// </summary>
		public ActionResult PaymentsByEmployeeGroups()
		{
			var pager = new InforoomModelFilter<Payment>(this);

			var selectedGroupId = 0;
			int.TryParse(pager.GetParam("selectedGroupId") ?? "", out selectedGroupId);
			var selectedEmployeeId = 0;
			int.TryParse(pager.GetParam("selectedEmployeeId") ?? "", out selectedEmployeeId);
			bool ignoreZero = true;
			bool.TryParse(pager.GetParam("selectedEmployeeId") ?? "", out ignoreZero);

			var selectedMonth = pager.GetParam("selectedMonth");
			var selectedYear = pager.GetParam("selectedYear");
			pager.ParamDelete("selectedYear");
			pager.ParamDelete("selectedMonth");
			DateTime dateFrom = SystemTime.Now().Date.FirstDayOfMonth();
			DateTime dateTo = SystemTime.Now().Date;
			DateTime selectedDate = DateTime.MinValue;
			if (!string.IsNullOrEmpty(selectedMonth) && !string.IsNullOrEmpty(selectedYear))
			{
				DateTime.TryParse($"01.{selectedMonth}.{selectedYear}", out selectedDate);
				if (selectedDate != DateTime.MinValue)
				{
					pager.ParamDelete("filter.GreaterOrEqueal.PaidOn");
					pager.ParamDelete("filter.LowerOrEqual.PaidOn");
					pager.ParamSet("filter.GreaterOrEqueal.PaidOn", selectedDate.Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
					pager.ParamSet("filter.LowerOrEqual.PaidOn", selectedDate.Date.LastDayOfMonth().ToString("dd.MM.yyyy"));
					dateFrom = selectedDate.Date.FirstDayOfMonth();
					dateTo = selectedDate.Date.LastDayOfMonth();
				}
			}
			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.PaidOn")) &&
					string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.PaidOn")))
			{
				pager.ParamDelete("filter.GreaterOrEqueal.PaidOn");
				pager.ParamDelete("filter.LowerOrEqual.PaidOn");
				pager.ParamSet("filter.GreaterOrEqueal.PaidOn", SystemTime.Now().Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.PaidOn", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
			}

			var currentEmployee = GetCurrentEmployee();
			var result = new List<ViewModelEmployeeGroupPaymentsReport>();
			var fileRes = PaymentsReport.GetGeneralReportEmployeesByGroups(DbSession, ref result, 
				dateFrom, dateTo, selectedGroupId, selectedEmployeeId, ignoreZero, pager.IsExportRequested(), currentEmployee);
			
			if (fileRes != null) {
				return File(fileRes, "application/ms-excel",
					$"Отчет по_платежам  - {SystemTime.Now().ToString("dd.MM.yyyy HH_mm")}.xls");
			}

			var dateA = DateTime.Parse(pager.GetParam("filter.LowerOrEqual.PaidOn")).AddDays(1).AddSeconds(-1);
			var dateB = DateTime.Parse(pager.GetParam("filter.LowerOrEqual.PaidOn")).AddDays(1).AddSeconds(-1);

			ViewBag.TotalSum = pager.TotalSumByFieldName("Sum");

			var firstYear = DbSession.Query<Payment>().OrderBy(s => s.Id).FirstOrDefault();
			var lastYear = DbSession.Query<Payment>().OrderByDescending(s => s.Id).FirstOrDefault();

			ViewBag.Pager = pager;
			ViewBag.ResultData = result;
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