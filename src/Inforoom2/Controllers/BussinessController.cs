using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Отображает страницы оферты
	/// </summary>
	public class BussinessController : BaseController
	{
		public ActionResult Index()
		{
			var ticket = new CallMeBackTicket();
			ViewBag.NewTicket = ticket;
			return View();
		}

		/// <summary>
		/// Обрабатывает отправку заявки на звонок
		/// </summary>
		[HttpPost]
		public ActionResult Index(CallMeBackTicket callMeBackTicket)
		{
			var errors = ValidationRunner.ValidateDeep(callMeBackTicket);
			if (errors.Length == 0)
			{
				DbSession.Save(callMeBackTicket);
				SuccessMessage("Заявка отправлена. В течении для вам перезвонят.");
				return RedirectToAction("Index");
			}

			ViewBag.NewTicket = callMeBackTicket;
			return View();
		}
	}
}