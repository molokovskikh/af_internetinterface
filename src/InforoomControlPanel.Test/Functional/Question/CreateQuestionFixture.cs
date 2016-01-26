using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Question
{
    class CreateQuestionFixture : QuestionFixture
    {
        [Test, Description("Успешное добавление вопроса-ответа")]
        public void SuccessfulCreateQuestion()
        {
            Open("Question/QuestionIndex");
            browser.FindElementByCssSelector(".QuestionIndex .entypo-plus").Click();
            browser.FindElementByCssSelector("textarea[id=question_Text]").SendKeys("Как поменять тариф?");
            browser.FindElementByCssSelector("textarea[id=question_Answer]").SendKeys("Смотрите в настройке личного кабинета");
            browser.FindElementByCssSelector(".btn.btn-green").Click();
            AssertText("Вопрос успешно отредактирован");
            var createQuestion = DbSession.Query<Inforoom2.Models.Question>().First(p => p.Text == "Как поменять тариф?");
            Assert.That(createQuestion, Is.Not.Null, "Вопрос-ответ должен сохраниться в базе данных");
            Assert.That(createQuestion.Answer, Is.EqualTo("Смотрите в настройке личного кабинета"), "Ответ на вопрос должен сохраниться в базе данных корректно");
        }

        [Test, Description("Неуспешное добавление вопроса-ответа")]
        public void UnsuccessfulCreateQuestion()
        {
            Open("Question/QuestionIndex");
            var questionCount = DbSession.Query<Inforoom2.Models.Question>().Count();
            browser.FindElementByCssSelector(".QuestionIndex .entypo-plus").Click();
            browser.FindElementByCssSelector(".btn.btn-green").Click();
            AssertText("Вопрос не может быть пустым");
            AssertText("Ответ не может быть пустым");
            var questionCountAfterTest = DbSession.Query<Inforoom2.Models.Question>().Count();
            Assert.That(questionCount, Is.EqualTo(questionCountAfterTest), "Вопрос-ответ должен сохраниться в базе данных");
        }
    }
}