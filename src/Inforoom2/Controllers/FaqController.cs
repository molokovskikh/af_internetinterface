
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
		public ActionResult Create(Ticket newTicket)
		{
			newTicket.Answer = "Без ответа";
			var errors = ValidationRunner.ValidateDeep(newTicket);
			if (errors.Length == 0) {
				DbSession.Save(newTicket);
				SuccessMessage("Вопрос успешно отправлен. Ждите ответа на почту.");
				return RedirectToAction("Index");
			}

			var questions = DbSession.Query<Question>().ToList();
			ViewBag.NewTicket = newTicket;
			ViewBag.Questions = questions;
			ViewBag.ShowQuestionForm = true;
			return View("Index");
			
		}
	}
}
