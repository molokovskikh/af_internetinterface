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
			if (questions.Count != 0) {
				ViewBag.Questions = questions;
			}
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
				var maxPriority = questions.Count != 0 ? questions.Max(k => k.Priority) : 0;
				question = new Question(maxPriority + 1);
			}
			ViewBag.Question = question;
			return View();
		}
		
		public ActionResult Move(int? questionId, string direction)
		{
			return ChangeModelPriority<Question>(questionId, direction, "AdminQuestionIndex", "AdminQuestion");
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

		public ActionResult DeleteQuestion(int? questionId)
		{
			var question = DbSession.Get<Question>(questionId);
			DbSession.Delete(question);
			return RedirectToAction("AdminQuestionIndex");
		}
	}
}