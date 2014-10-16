using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;
using NHibernate.Proxy;

namespace Inforoom2.Controllers
{
	/// <summary>
	/// Страница управления вопросами и ответами
	/// </summary>
	public class AdminQuestionController : AdminController
	{
		public ActionResult AdminQuestionIndex()
		{
			var questions = DbSession.Query<Question>().OrderBy(k => k.Priority).ToList();
			var maxPriority = questions.Max(k => k.Priority);
			var question = new Question(maxPriority + 1);
			ViewBag.Questions = questions;
			ViewBag.Question = question;
			return View();
		}

		public ActionResult EditQuestion(int? questionId)
		{
			Question question;
			if (questionId != null) {
				question = DbSession.Get<Question>(questionId);
			}
			else {
				var questions = DbSession.Query<Question>().OrderBy(k => k.Priority).ToList();
				var maxPriority = questions.Max(k => k.Priority);
				question = new Question(maxPriority + 1);
			}
			ViewBag.Question = question;
			return View();
		}

		public ActionResult CreateQuestion()
		{
			return View(ViewBag.Question);
		}

		public ActionResult Move(int? questionId, string direction)
		{
			var question = DbSession.Get<Question>(questionId);
			IList<Question> questions;
			Question targetQuestion;
			if (direction == "Up") {
				questions = DbSession.Query<Question>().OrderByDescending(k => k.Priority).ToList();
				targetQuestion = questions.FirstOrDefault(k => k.Priority < question.Priority);
			}
			else {
				questions = DbSession.Query<Question>().OrderBy(k => k.Priority).ToList();
				targetQuestion = questions.FirstOrDefault(k => k.Priority > question.Priority);
			}
			if (targetQuestion != null) {
				int targetPerriority = targetQuestion.Priority;
				targetQuestion.Priority = question.Priority;
				question.Priority = targetPerriority;
				DbSession.Save(question);
				DbSession.Save(targetQuestion);
			}
			return RedirectToAction("AdminQuestionIndex");
		}

		[HttpPost]
		public ActionResult UpdateQuestion(Question question)

		{
			ViewBag.Question = question;
			var errors = ValidationRunner.ValidateDeep(question);
			if (errors.Length == 0) {
				DbSession.SaveOrUpdate(question);
				SuccessMessage("Вопрос успешно отредактирован");
			}
			else {
				ErrorMessage("Что-то пошло не так");
				return View("EditQuestion");
			}

			return RedirectToAction("AdminQuestionIndex");
		}
	}
}