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
	public class WriteOffsReport
	{
		public static byte[] GetGeneralReport(Controller controller,
			ref InforoomModelFilter<WriteOff> pager,
			bool dateNecessary = true)
		{
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Client.Id", OrderingDirection.Asc);
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
				if (pager.GetParam("filter.IsNotNull.Client.PhysicalClient") != null)
					header.Add(string.Format("Тип клиента: {0}",
						pager.GetParam("filter.IsNotNull.Client.PhysicalClient") == "1"
							? "Физ.лицо"
							: pager.GetParam("filter.IsNotNull.Client.PhysicalClient") != ""
								? "Юр.лицо"
								: "Все"))
						;
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

				//формирование блока выгрузки
				pager.SetExportFields(
					s => new
					{
						ЛС = s.Client.Id,
						Клиент = s.Client.Fullname,
						Регион = s.Client.GetRegion().Name,
						Сумма = s.WriteOffSum,
						Дата = s.WriteOffDate,
						Комментарий = s.Comment != null ? s.Comment : ""
					}, complexLinq: true);
				//выгрузка в файл
                return pager.ExportToExcelFile();
            }
            controller.ViewBag.Pager = pager;
            return null;
        }
	}
}