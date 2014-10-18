using System;
using System.Linq;
using System.Web.UI.WebControls;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional
{
	[TestFixture]
	public class FaqFixture : BaseFixture
	{
		protected Question Question;

		[SetUp]
		public void FixtureSetup()
		{
			var question = new Question();
			question.Answer = "Test answer";
			question.Text = "test question text";
			question.IsPublished = true;
			question.Priority = question.GetNextPriority(session);

			session.Save(question);
			session.Flush();
			Question = question;
		}


		[Test, Description("Проверка возможности просматривать вопросы и ответы")]
		public void QuestionsTest()
		{	
			Open("Faq");
			//Проверяем наличие вопросов
			AssertText(Question.Text);

			//Проверяем наличие ответов при клике
			AssertNoText(Question.Answer);
			var answerButtons = browser.FindElementsByCssSelector(".ShowAnswer");
			foreach(var button in answerButtons)
				button.Click();
			AssertText(Question.Answer);

			//Скрываем ответы
			foreach(var button in answerButtons)
				button.Click();
			AssertNoText(Question.Answer);
		}

		[Test, Description("Проверка возможности отпрявлять свои вопросы")]
		public void TicketTest()
		{	
			Open("Faq");
			var testEmail = "test@analit.net";
			var testQuestion = "Когда выйдут новые тарифы(тест)?";

			var showButton = browser.FindElementByCssSelector(".ShowQuestionForm");
			var hideButton = browser.FindElementByCssSelector(".HideQuestionForm");
			var email = browser.FindElementByCssSelector("input[name='newTicket.Email']");
			var text = browser.FindElementByCssSelector("textarea[name='newTicket.Text']");
			var applyButton = browser.FindElementByCssSelector("input[type='submit']");

			//проверяем появление исчезновение
			Assert.That(applyButton.Displayed, Is.False);
			showButton.Click();
			Assert.That(applyButton.Displayed, Is.True);
			hideButton.Click();
			Assert.That(applyButton.Displayed, Is.False);

			//проверяем отправку вопроса
			showButton.Click();
			email.SendKeys(testEmail);
			text.SendKeys(testQuestion);
			applyButton.Click();

			var ticket = session.Query<Ticket>().OrderByDescending(i=>i.Id).First();
			Assert.That(ticket.Text,Is.EqualTo(testQuestion));
			Assert.That(ticket.Email,Is.EqualTo(testEmail));
		}
	}
}