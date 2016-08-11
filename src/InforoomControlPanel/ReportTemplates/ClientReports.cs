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
	public class ClientReports
	{
		public static InforoomModelFilter<Client> GetGeneralReport(Controller controller, InforoomModelFilter<Client> pager,
			bool dateNecessary = true)
		{
			if (!string.IsNullOrEmpty(pager.GetParam("filter.Equal.RentalHardwareList.First().Hardware.Name")) &&
			    !string.IsNullOrEmpty(pager.GetParam("withArchiveRents"))) {
				var stWithArchiveRents = pager.GetParam("withArchiveRents").Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
				var withArchiveRents = false;
				if (stWithArchiveRents != null && bool.TryParse(stWithArchiveRents, out withArchiveRents) && withArchiveRents) {
					pager.ParamDelete("filter.Equal.RentalHardwareList.First().IsActive");
					pager.ParamSet("filter.Equal.RentalHardwareList.First().IsActive", "true");
					controller.ViewBag.WithArchiveRents = true;
				}
				else {
					pager.ParamDelete("filter.Equal.RentalHardwareList.First().IsActive");
				}
			}
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			if (dateNecessary && string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.CreationDate")) &&
			    string.IsNullOrEmpty(pager.GetParam("filter.LowerOrEqual.CreationDate"))) {
				pager.ParamDelete("filter.GreaterOrEqueal.CreationDate");
				pager.ParamDelete("filter.LowerOrEqual.CreationDate");
				pager.ParamSet("filter.GreaterOrEqueal.CreationDate", SystemTime.Now().Date.FirstDayOfMonth().ToString("dd.MM.yyyy"));
				pager.ParamSet("filter.LowerOrEqual.CreationDate", SystemTime.Now().Date.ToString("dd.MM.yyyy"));
			}
			var criteria = pager.GetCriteria();
			if (pager.IsExportRequested()) {
				//формирование шапки (отправляем значение строк в обратном порядке)
				var header = new List<string>();
				//строки между шапкой и содержанием 
				header.Add("");
				header.Add("Всего строк: " + pager.TotalItems);
				header.Add("");
				//поля шапки
				//Тип 
				if (!string.IsNullOrEmpty(pager.GetParam("filter.IsNotNull.PhysicalClient"))) {
					var headerText = pager.GetParam("filter.IsNotNull.PhysicalClient") == "1" ? "Физ.лицо" : "Юр.лицо";
					header.Add($"Тип клиента: {headerText}");
				}
				else {
					header.Add($"Тип клиента: все");
				}
				//Проверен
				if (!string.IsNullOrEmpty(pager.GetParam("filter.Equal.PhysicalClient.Checked"))) {
					var headerText = pager.GetParam("filter.Equal.PhysicalClient.Checked") == "1" ? "Да" : "Нет";
					header.Add($"Проверен: {headerText}");
				}
				else {
					header.Add("Проверен: не указано");
				}
				//Оборудование
				if (!string.IsNullOrEmpty(pager.GetParam("filter.Equal.RentalHardwareList.First().Hardware.Name"))) {
					var headerText = pager.GetParam("filter.Equal.RentalHardwareList.First().Hardware.Name");
					header.Add($"Оборудование: {headerText}");
				}
				else {
					header.Add("Оборудование: не указано");
				}

				//Регион
				if (!string.IsNullOrEmpty(pager.GetParam("clientregionfilter."))) {
					var headerText = pager.GetParam("clientregionfilter.").Replace("-", "").Replace(" ", "");
					header.Add($"Регион: {headerText}");
				}
				else {
					header.Add("Регион: все");
				}
				//Тариф
				if (!string.IsNullOrEmpty(pager.GetParam("filter.Equal.PhysicalClient.Plan.Name"))) {
					var headerText = pager.GetParam("filter.Equal.PhysicalClient.Plan.Name");
					header.Add($"Тариф: {headerText}");
				}
				else {
					header.Add("Тариф: не указано");
				}
				//Статус	
				if (!string.IsNullOrEmpty(pager.GetParam("filter.Equal.Status.Name"))) {
					header.Add($"Статус: {pager.GetParam("filter.Equal.Status.Name")}");
				}
				else {
					header.Add("Статус: не указано");
				}
				//Регистратор
				if (!string.IsNullOrEmpty(pager.GetParam("filter.Equal.WhoRegistered.Name"))) {
					header.Add($"Регистратор: {pager.GetParam("filter.Equal.WhoRegistered.Name")}");
				}
				else {
					header.Add("Регистратор: не указано");
				}
				//Дата регистрации
				if (!string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.CreationDate"))
				    || !string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.CreationDate"))) {
					header.Add(
						$"Дата регистрации: с {pager.GetParam("filter.GreaterOrEqueal.CreationDate")} по {pager.GetParam("filter.LowerOrEqual.CreationDate")}");
				}
				else {
					header.Add("Дата регистрации: не указано");
				}

				header.Add("");
				header.Add("Отчет по клиентам - " + SystemTime.Now().ToString("dd.MM.yyyy HH:mm"));
				pager.SetHeaderLines(header);
				//формирование блока выгрузки
				pager.SetExportFields(s => new
				{
                    ЛС = s.Id,
					Клиент = s.Fullname,
					Адрес = s.GetAddress() ?? "",
					Контакты = "' "+string.Join(", ", s.Contacts.OrderBy(c => c.Type).Select(c => c.ContactString).ToList()),
					Дата_регистрации = s.CreationDate.HasValue ? s.CreationDate.Value.ToString("dd.MM.yyyy") : "",
					Дата_расторжения =
						s.GetDissolveDate().HasValue ? s.GetDissolveDate().Value.ToString("dd.MM.yyyy") : "",
					Тариф = s.PhysicalClient != null && s.PhysicalClient.Plan != null ? s.PhysicalClient.Plan.NameWithPrice : s.LegalClient.Plan.ToString(),
					Баланс = s.Balance,
					Статус = s.Status.Name,
					Дата_блокировки = s.StatusChangedOn.HasValue ? s.StatusChangedOn.Value.ToString("dd.MM.yyyy") : "",
					Коммутатор = string.Join(", ", s.Endpoints.Where(d=>!d.Disabled).Select(c => c.Switch.Name).ToList()),
					Оборудование = string.Join(", ", s.RentalHardwareList.Where(a => a.IsActive).Select(c => c.Hardware.Name).ToList())
				}, complexLinq: true);
				//выгрузка в файл
				pager.ExportToExcelFile(controller.ControllerContext.HttpContext,
					"Отчет по клиентам - " + SystemTime.Now().ToString("dd.MM.yyyy HH_mm"));
				return null;
			}
			controller.ViewBag.Pager = pager;
			return pager;
		}
	}
}