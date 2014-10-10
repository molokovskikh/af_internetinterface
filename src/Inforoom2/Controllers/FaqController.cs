
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
			var questions = DBSession.Query<Question>().ToList();
			var question = new Question();

			ViewBag.NewQuestion = question;
			ViewBag.Questions = questions;
			return View();
		}

		/// <summary>
		/// Обрабатывает отправку нового вопроса
		/// </summary>
		[HttpPost]
		public ActionResult Create(Question newQuestion)
		{
			var errors = ValidationRunner.ValidateDeep(newQuestion);
			if (errors.Length == 0) {
				DBSession.Save(newQuestion);
				SuccessMessage("Вопрос успешно отправлен. Ждите ответа на почту.");
				return RedirectToAction("Index");
			}

			var questions = DBSession.Query<Question>().ToList();
			ViewBag.NewQuestion = newQuestion;
			ViewBag.Questions = questions;
			ViewBag.ShowQuestionForm = true;
			return View("Index");
			
		}
	}
}
