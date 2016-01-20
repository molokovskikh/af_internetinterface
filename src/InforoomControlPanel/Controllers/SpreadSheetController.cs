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
			pager = WriteOffsReport.GetGeneralReport(this, pager);
			if (pager == null) {
				return null;
			}
			return View();
		}
		 
	}
}