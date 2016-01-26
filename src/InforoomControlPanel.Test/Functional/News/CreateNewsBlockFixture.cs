using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.News
{
    class CreateNewsBlockFixture : NewsFixture
    {
        [Test, Description("Успешное добавление новостного блока")]
        public void SuccessfulCreateNewsBlock()
        {
            Open("News/NewsIndex");
            browser.FindElementByCssSelector(".NewsIndex .entypo-plus").Click();
            browser.FindElementByCssSelector("input[class=NewsVersionCheckbox]").Click();
            browser.FindElementByCssSelector("input[id=newsBlock_Title]").SendKeys("Главная новость");
            browser.FindElementByCssSelector("textarea[id=newsBlock_Preview]").SendKeys("Новые события");
            browser.FindElementByCssSelector("textarea[id=newsBlock_Body]").SendKeys("новостной блок");
            browser.FindElementByCssSelector("input[id=newsBlock_Url]").SendKeys("Url-новости");
            browser.FindElementByCssSelector("input[id=newsBlock_IsPublished]").Click();
            browser.FindElementByCssSelector(".btn.btn-green").Click();
            AssertText("Новость успешно сохранена");
            var createNewsBlock = DbSession.Query<NewsBlock>().First(p => p.Title == "Главная новость");
            Assert.That(createNewsBlock, Is.Not.Null, "Новостной блок должен сохраниться в базе данных");
        }

        [Test, Description("Неуспешное добавление новостного блока")]
        public void UnsuccessfulCreateNewsBlock()
        {
            Open("News/NewsIndex");
            var newsBlockCount = DbSession.Query<NewsBlock>().Count();
            browser.FindElementByCssSelector(".NewsIndex .entypo-plus").Click();
            browser.FindElementByCssSelector(".btn.btn-green").Click();
            AssertText("Заголовок не может быть пустым");
            AssertText("Короткое описание не может быть пустым");
            AssertText("Описание не может быть пустым");
            var newsBlocCountkAfterTest = DbSession.Query<NewsBlock>().Count();
            Assert.That(newsBlocCountkAfterTest, Is.EqualTo(newsBlockCount), "Новостной блок не должен сохраниться в базе данных");
        }
    }
}