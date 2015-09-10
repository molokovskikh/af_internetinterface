using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Question
{
    class EditQuestionFixture : QuestionFixture
    {
        [Test, Description("Изменение вопроса-ответа")]
        public void NetworkNodeEdit()
        {
            Open("Question/QuestionIndex");
            var questionEdit = DbSession.Query<Inforoom2.Models.Question>().First(p => p.Text == "Могу ли я одновременно пользоваться интернетом на нескольких компьютерах, если у меня один кабель?");
            var targetQuestion = browser.FindElementByXPath("//td[contains(.,'" + questionEdit.Text + "')]");
            var row = targetQuestion.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-success"));
            button.Click();
            browser.FindElementByCssSelector("textarea[id=question_Text]").Clear();
            browser.FindElementByCssSelector("textarea[id=question_Text]").SendKeys("Как зайти в личный кабинет?");
            browser.FindElementByCssSelector("textarea[id=question_Answer]").Clear();
            browser.FindElementByCssSelector("textarea[id=question_Answer]").SendKeys("Ответ отправлен на почту");
            browser.FindElementByCssSelector("input[id=question_IsPublished]").Click();
            browser.FindElementByCssSelector(".btn.btn-green").Click();
            AssertText("Вопрос успешно отредактирован");
            DbSession.Refresh(questionEdit);
            Assert.That(questionEdit.Text, Is.StringContaining("Как зайти в личный кабинет?"), "Изменения в вопросе должны сохраниться и в базе данных");
            Assert.That(questionEdit.IsPublished, Is.False, "Изменения маркера опубликованности вопроса должны сохраниться и в базе данных");
            Assert.That(questionEdit.Answer, Is.StringContaining("Ответ отправлен на почту"), "Изменения в ответе на вопрос должны сохраниться и в базе данных");
        }
    }
}