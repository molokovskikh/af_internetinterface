
using System;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Models;
using NHibernate.Linq;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Страница вопросов и ответов
	/// </summary>
	public class FaqController : BaseController
	{

		/// <summary>
		/// Отображает список вопросов и ответов, а также форму нового вопроса
		/// </summary>
		public ActionResult Index()
		{
			var questions = DbSession.Query<Question>().Where(k => k.IsPublished).OrderBy(k=>k.Priority).ToList();
			var ticket = new Ticket();
			ViewBag.NewTicket = ticket;
			ViewBag.Questions = questions;
			return View();
		}

		/// <summary>
		/// Обрабатывает отправку нового вопроса
		/// </summary>
		[HttpPost]
		public ActionResult Index(Ticket ticket)
		{
			Index();
			ViewBag.NewTicket = ticket;
			var errors = ValidationRunner.ValidateDeep(ticket);
			if (errors.Length == 0) {
				DbSession.Save(ticket);
				SuccessMessage("Вопрос успешно отправлен. Ответ придет вам на на почту.");
				ViewBag.NewTicket = new Ticket();
			}
			else
				ViewBag.ShowQuestionForm = true;
			return View();
		}

		/// <summary>
		/// Страница техподдержки
		/// </summary>
		public ActionResult TechSupport()
		{
			Index();
			return View();
		}

		/// <summary>
		/// Обрабатывает отправку нового вопроса
		/// </summary>
		[HttpPost]
		public ActionResult TechSupport(Ticket ticket)
		{
			Index(ticket);
			return View();
		}
	}
}
