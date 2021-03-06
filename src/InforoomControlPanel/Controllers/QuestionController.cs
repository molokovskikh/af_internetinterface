﻿using System.Linq;
using System.Web;
using System.Web.Mvc;
using Inforoom2.Components;
using Inforoom2.Models;
using NHibernate.Linq;

namespace InforoomControlPanel.Controllers
{
	/// <summary>
	/// Страница управления вопросами и ответами
	/// </summary>
	public class QuestionController : ControlPanelController
	{
		public QuestionController()
		{
			ViewBag.BreadCrumb = "Вопросы";
		}

		public  ActionResult Index()
		{
			return QuestionIndex();
		}

		/// <summary>
		/// Страница списка вопросов
		/// </summary>
		public ActionResult QuestionIndex()
		{
			var questions = DbSession.Query<Question>().OrderBy(k => k.Priority).ToList();
			if (questions.Count != 0) {
				ViewBag.Questions = questions;
			}
			return View("QuestionIndex");
		}

		/// <summary>
		/// Изменение вопросов
		/// </summary>
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

		/// <summary>
		///Приоритет отображения вопросов
		/// </summary>
		public ActionResult Move(int? questionId, string direction)
		{
			return ChangeModelPriority<Question>(questionId, direction, "QuestionIndex", "Question");
		}

		/// <summary>
		/// Изменение вопросов
		/// </summary>
		[ValidateInput(false), HttpPost]
		public ActionResult UpdateQuestion([EntityBinder] Question question)
		{
			//question.Answer = HttpUtility.HtmlEncode(question.Answer);
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

			return RedirectToAction("QuestionIndex");
		}

		/// <summary>
		/// Удаление вопроса
		/// </summary>
		public ActionResult DeleteQuestion(int? questionId)
		{
			var question = DbSession.Get<Question>(questionId);
			DbSession.Delete(question);
			return RedirectToAction("QuestionIndex");
		}
	}
}