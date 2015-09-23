using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using Inforoom2.Components;
using Inforoom2.Controllers;
using Inforoom2.Models;
using NHibernate.Linq;
using NHibernate.Mapping;
using NHibernate.Util;
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
			var criteria = pager.GetCriteria();
			if (pager.IsExportRequested()) {
				//pager.SetExportFields(s => new { ЛС = s.Id, Клиент = s.Fullname, Тариф = s.PhysicalClient.Plan.Name, Адрес = s.PhysicalClient.Address.FullAddress, Статус = s.Status.Name });
				pager.SetExportFields(s => new
				{
					ЛС = s.Id,
					Клиент = s.Fullname,
					Тариф = s.PhysicalClient != null && s.PhysicalClient.Plan != null ? s.PhysicalClient.Plan.Name : "Нет",
					Адрес = s.Address == null ? s._oldAdressStr ?? "" : s.Address.FullAddress,
					Статус = s.Status.Name
				}, complexLinq: true);
				pager.ExportToExcelFile(ControllerContext.HttpContext);
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
			var criteria = pager.GetCriteria();

			if (pager.IsExportRequested()) {
				pager.SetExportFields(s => new { ЛС = s.Client.Id, Клиент = s.Client.Fullname, Дата = s.WriteOffDate, Сумма = s.WriteOffSum });
				pager.ExportToExcelFile(ControllerContext.HttpContext);
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