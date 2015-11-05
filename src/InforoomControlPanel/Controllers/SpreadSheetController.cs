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
			if (string.IsNullOrEmpty(pager.GetParam("orderBy")))
				pager.SetOrderBy("Id", OrderingDirection.Desc);
			if (string.IsNullOrEmpty(pager.GetParam("filter.GreaterOrEqueal.CreationDate")) &&
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
				if (pager.GetParam("filter.IsNotNull.PhysicalClient") != null)
					header.Add(string.Format("Тип клиента: {0}", pager.GetParam("filter.IsNotNull.PhysicalClient") == "1" ? "Физ.лицо" : "Юр.лицо"));
				//Проверен
				if (pager.GetParam("filter.Equal.PhysicalClient.Checked") != null)
					header.Add(string.Format("Проверен: {0}", pager.GetParam("filter.Equal.PhysicalClient.Checked") == "1" ? "Да" : "Нет"));
				//Оборудование
				if (pager.GetParam("filter.Equal.RentalHardwareList.First().Hardware.Name") != null)
					header.Add(string.Format("Оборудование: {0}", pager.GetParam("filter.Equal.RentalHardwareList.First().Hardware.Name")));
				//Регион
				if (pager.GetParam("clientregionfilter.") != null)
					header.Add(string.Format("Регион: {0}",
						string.IsNullOrEmpty(pager.GetParam("clientregionfilter.").Replace("-", "").Replace(" ", ""))
							? pager.GetParam("clientregionfilter.") : " Все "));
				//Тариф
				if (pager.GetParam("filter.Equal.PhysicalClient.Plan.Name") != null)
					header.Add(string.Format("Тариф: {0}", pager.GetParam("filter.Equal.PhysicalClient.Plan.Name")));
				//Статус	
				if (pager.GetParam("filter.Equal.Status.Name") != null)
					header.Add(string.Format("Статус: {0}", pager.GetParam("filter.Equal.Status.Name")));
				//Регистратор
				if (pager.GetParam("filter.Equal.WhoRegistered.Name") != null)
					header.Add(string.Format("Регистратор: {0}", pager.GetParam("filter.Equal.WhoRegistered.Name")));
				//Дата регистрации
				if (pager.GetParam("filter.GreaterOrEqueal.CreationDate") != null
				    || pager.GetParam("filter.LowerOrEqual.CreationDate") != null)
					header.Add(string.Format("Дата регистрации: с {0} по {1}",
						pager.GetParam("filter.GreaterOrEqueal.CreationDate"), pager.GetParam("filter.LowerOrEqual.CreationDate")));

				header.Add("");
				header.Add("Отчет по клиентам - " + SystemTime.Now().ToString("dd.MM.yyyy HH:mm"));
				pager.SetHeaderLines(header);
				//формирование блока выгрузки
				pager.SetExportFields(s => new {
					ЛС = s.Id,
					Клиент = s.Fullname,
					Адрес = s.GetAddress() ?? "",
					Контакты = string.Join(", ", s.Contacts.OrderBy(c => c.Type).Select(c => c.ContactString).ToList()),
					Дата_регистрации = s.CreationDate.HasValue ? s.CreationDate.Value.ToString("dd.MM.yyyy") : "",
					Дата_расторжения = s.PhysicalClient == null && s.LegalPersonOrders != null ? (s.LegalPersonOrders.Where(o => o.IsDeactivated && o.EndDate != null).ToList().OrderByDescending(f => f.EndDate.Value).FirstOrDefault()
					                                                                              ?? new ClientOrder() { EndDate = Convert.ToDateTime("0001-01-01 00:00:00") }).EndDate.Value.ToString("dd.MM.yyyy").Replace("01.01.0001", "") :
						(s.Status.Type == StatusType.Dissolved ? s.StatusChangedOn.HasValue ? s.StatusChangedOn.Value.ToString("dd.MM.yyyy") : "" : ""),
					Тариф = s.PhysicalClient != null && s.PhysicalClient.Plan != null ? s.PhysicalClient.Plan.NameWithPrice : "Нет",
					Баланс = s.Balance,
					Статус = s.Status.Name,
					Дата_блокировки = s.StatusChangedOn.HasValue ? s.StatusChangedOn.Value.ToString("dd.MM.yyyy") : "",
					Коммутатор = string.Join(", ", s.Endpoints.Select(c => c.Switch.Name).ToList()),
					Оборудование = string.Join(", ", s.RentalHardwareList.Select(c => c.Hardware.Name).ToList())
				}, complexLinq: true);
				//выгрузка в файл
				pager.ExportToExcelFile(ControllerContext.HttpContext, "Отчет по клиентам - " + SystemTime.Now().ToString("dd.MM.yyyy HH_mm"));
				return null;
			}
			ViewBag.Pager = pager;
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
					header.Add(string.Format("Тип клиента: {0}", pager.GetParam("filter.IsNotNull.PhysicalClient") == "1" ? "Физ.лицо" : "Юр.лицо"));
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

				pager.SetExportFields(s => new { ЛС = s.Client.Id, Клиент = s.Client.Fullname, Дата = s.WriteOffDate, Сумма = s.WriteOffSum });
				pager.ExportToExcelFile(ControllerContext.HttpContext, "Отчет по списаниям - " + SystemTime.Now().ToString("dd.MM.yyyy HH_mm"));
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