using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom2.Components;
using Inforoom2.Models;
using Remotion.Linq.Clauses;

namespace InforoomControlPanel.ReportTemplates
{
	public class PaymentsReport
	{
		public static InforoomModelFilter<Payment> GetGeneralReport(Controller controller,
			InforoomModelFilter<Payment> pager,
			bool dateNecessary = true)
		{
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("PaidOn", OrderingDirection.Asc);

			pager.ParamDelete("filter.Equal.IsDuplicate");
			pager.ParamSet("filter.Equal.IsDuplicate", false.ToString());

			var selectedMonth = pager.GetParam("selectedMonth");
			var selectedYear = pager.GetParam("selectedYear");
			pager.ParamDelete("selectedYear");
			pager.ParamDelete("selectedMonth");
			DateTime selectedDate = DateTime.MinValue;
			if (!string.IsNullOrEmpty(selectedMonth) && !string.IsNullOrEmpty(selectedYear)) {
				DateTime.TryParse($"01.{selectedMonth}.{selectedYear}", out selectedDate);
				if (selectedDate != DateTime.MinValue) {
					pager.ParamDelete("filter.GreaterOrEqueal.PaidOn");
					pager.ParamDelete("filter.LowerOrEqual.PaidOn");
					pager.ParamSet("filter.GreaterOrEqueal.PaidOn", selectedDate.Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
					pager.ParamSet("filter.LowerOrEqual.PaidOn", selectedDate.Date.LastDayOfMonth().ToString("dd.MM.yyyy"));
				}
			}

			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.PaidOn")) &&
			    string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.PaidOn"))) {
				pager.ParamDelete("filter.GreaterOrEqueal.PaidOn");
				pager.ParamDelete("filter.LowerOrEqual.PaidOn");
				pager.ParamSet("filter.GreaterOrEqueal.PaidOn", SystemTime.Now().Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.PaidOn", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
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
				if (pager.GetParam("filter.IsNotNull.Client.PhysicalClient") != null)
					header.Add(string.Format("Тип клиента: {0}",
						pager.GetParam("filter.IsNotNull.Client.PhysicalClient") == "1"
							? "Физ.лицо"
							: pager.GetParam("filter.IsNotNull.Client.PhysicalClient") != "" ? "Юр.лицо" : "Все"));
				//Регион
				if (pager.GetParam("clientregionfilter.Client") != null)
					header.Add(string.Format("Регион: {0}", pager.GetParam("clientregionfilter.Client")));
				//Дата списания
				if (pager.GetParam("filter.GreaterOrEqueal.PaidOn") != null)
					header.Add(string.Format("Дата поступления: {0}", pager.GetParam("filter.GreaterOrEqueal.PaidOn")));
				//поля шапки
				header.Add("");
				header.Add("Отчет по списаниям - " + SystemTime.Now().ToString("dd.MM.yyyy HH:mm"));
				pager.SetHeaderLines(header);

				//формирование блока выгрузки
				pager.SetExportFields(
					s => new
					{
						ЛС = s.Client.Id,
						Клиент = s.Client.Fullname,
						Регион = s.Client.GetRegion().Name,
						Сумма = s.Sum,
						Дата = s.PaidOn,
						Бонусный = s.Virtual == true ? "Да" : "Нет",
						Комментарий = s.Comment != null ? s.Comment : ""
					}, complexLinq: true);
				//выгрузка в файл
				pager.ExportToExcelFile(controller.ControllerContext.HttpContext,
					"Отчет по платежам - " + SystemTime.Now().ToString("dd.MM.yyyy HH_mm"));
				return null;
			}
			controller.ViewBag.Pager = pager;
			return pager;
		}
	}
}