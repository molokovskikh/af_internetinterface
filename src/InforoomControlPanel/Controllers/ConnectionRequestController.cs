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
using Inforoom2.Helpers;
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
	public class ConnectionRequestController : ControlPanelController
	{
		/// <summary>
		/// Отображает форму новой заявки
		/// </summary>
		[HttpGet]
		public ActionResult ConnectionRequestEdit(int id)
		{
			var connectionRequest = DbSession.Query<ClientRequest>().FirstOrDefault(s => s.Id == id);
			return View(connectionRequest);
		}

		/// <summary>
		/// Отображает форму новой заявки
		/// </summary>
		[HttpPost]
		public ActionResult AddConnectionRequestComment(int id, string comment)
		{
			if (string.IsNullOrEmpty(comment)) {
				ErrorMessage("Комментрий не добавлен: текст отсутствует");
				return RedirectToAction("ConnectionRequestEdit", new {id});
			}
			var connectionRequest = DbSession.Query<ClientRequest>().FirstOrDefault(s => s.Id == id);
			if (connectionRequest != null) {
				var newComment = new ConnectionRequestComment()
				{
					Date = SystemTime.Now(),
					Registrator = GetCurrentEmployee(),
					Request = connectionRequest,
					Comment = comment.ReplaceSharpWithRedmine()
			};
				DbSession.Save(newComment);
                connectionRequest.ConnectionRequestComments.Add(newComment);
				DbSession.Save(connectionRequest);
			}
			return RedirectToAction("ConnectionRequestEdit", new {id});
		}
	}
}