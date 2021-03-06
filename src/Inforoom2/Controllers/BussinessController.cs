﻿using System.Web.Mvc;
using Inforoom2.Models;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Отображает страницы оферты
	/// </summary>
	public class BussinessController : Inforoom2Controller
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
			var errors = ValidationRunner.Validate(callMeBackTicket);
			//проверка капчи 
			if (CurrentClient == null && (callMeBackTicket.Captcha==null || (callMeBackTicket.Captcha.Length < 4
										  || (HttpContext.Session["captcha"].ToString()).IndexOf(callMeBackTicket.Captcha) == -1)))
			{
				callMeBackTicket.Captcha = "";
			}
			else {
				errors.RemoveErrors("CallMeBackTicket", "Captcha");
			} 

			if (errors.Length == 0)
			{ 
				DbSession.Save(callMeBackTicket);
				SuccessMessage("Заявка отправлена. В течении для вам перезвонят.");
				return new RedirectResult(Url.Action("Index") + "#notification");
			}

			ViewBag.NewTicket = callMeBackTicket;
			//если на странице бизнеса отправить заявку на звонок, то не должно появляться окно
			if (Request.Params["bussiness"] != null)
				AddJavascriptParam("CallMeBack", "0");
			return View();
		}
	}
}