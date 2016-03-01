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