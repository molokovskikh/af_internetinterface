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
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.WriteOffDate")) &&
			    string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.WriteOffDate"))) {
				pager.ParamDelete("filter.GreaterOrEqueal.WriteOffDate");
				pager.ParamDelete("filter.LowerOrEqual.WriteOffDate");
				pager.ParamSet("filter.GreaterOrEqueal.WriteOffDate", SystemTime.Now().Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.WriteOffDate", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
			}
			var criteria = pager.GetCriteria();

			if (pager.IsExportRequested()) {
				//формирование шапки (отправляем значение строк в обратном порядке)
				var header = new List<string>();
				//строки между шапкой и содержанием 
				header.Add("");
				header.Add("Всего строк: " + pager.TotalItems);
				header.Add("");
				//Тип 
				if (pager.GetParam("filter.IsNotNull.PhysicalClient") != null)
					header.Add(string.Format("Тип клиента: {0}",
						pager.GetParam("filter.IsNotNull.PhysicalClient") == "1" ? "Физ.лицо" : "Юр.лицо"));
				//Регион
				if (pager.GetParam("clientregionfilter.Client") != null)
					header.Add(string.Format("Регион: {0}", pager.GetParam("clientregionfilter.Client")));
				//Дата списания
				if (pager.GetParam("filter.GreaterOrEqueal.WriteOffDate") != null)
					header.Add(string.Format("Дата списания: {0}", pager.GetParam("filter.GreaterOrEqueal.WriteOffDate")));
				//поля шапки
				header.Add("");
				header.Add("Отчет по списаниям - " + SystemTime.Now().ToString("dd.MM.yyyy HH:mm"));
				pager.SetHeaderLines(header);

				pager.SetExportFields(
					s => new {ЛС = s.Client.Id, Клиент = s.Client.Fullname, Дата = s.WriteOffDate, Сумма = s.WriteOffSum});
				pager.ExportToExcelFile(ControllerContext.HttpContext,
					"Отчет по списаниям - " + SystemTime.Now().ToString("dd.MM.yyyy HH_mm"));
				return null;
			}
			ViewBag.Pager = pager;
			return View();
		}

		///// <summary>
		///// Отчет по выручке на ежедневной основе
		///// </summary>
		//public ActionResult Index()
		//{
		//	return View();
		//}
		///// <summary>
		///// Отчет по существующей абонентской базе
		///// </summary>
		//public ActionResult Index()
		//{
		//	return View();
		//}
	}
}