using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Question
{
    class DeleteQuestionFixture : QuestionFixture
    {
        [Test, Description("Удаление вопроса-ответа")]
        public void DeleteNewsBlock()
        {
            Open("Question/QuestionIndex");
            var questionCount = DbSession.Query<Inforoom2.Models.Question>().Count();
            var question = DbSession.Query<Inforoom2.Models.Question>().First(p => p.Text == "Могу ли я одновременно пользоваться интернетом на нескольких компьютерах, если у меня один кабель?");
            var targetQuestion = browser.FindElementByXPath("//td[contains(.,'" + question.Text + "')]");
            var row = targetQuestion.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector(".entypo-cancel-circled"));
            button.Click();
            var questionCountAfterTest = DbSession.Query<Inforoom2.Models.Question>().Count();
            Assert.That(questionCountAfterTest, Is.EqualTo(questionCount - 1), "Вопрос-ответ должен удалиться в базе данных");
        }
    }
}