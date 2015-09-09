using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.News
{
    class EditNewsBlockFixture : NewsFixture
    {
        [Test, Description("Изменение новостного блока")]
        public void EditNewsBlock()
        {
            Open("News/NewsIndex");
            var newsBlock = DbSession.Query<NewsBlock>().First(p => p.Title == "Новость2");
            var targetNewsBlock = browser.FindElementByXPath("//td[contains(.,'" + newsBlock.Title + "')]");
            var row = targetNewsBlock.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector(".entypo-vcard"));
            button.Click();
            browser.FindElementByCssSelector("input[class=NewsVersionCheckbox]").Click();
            browser.FindElementByCssSelector("input[id=newsBlock_Title]").Clear();
            browser.FindElementByCssSelector("input[id=newsBlock_Title]").SendKeys("Главная новость");
            browser.FindElementByCssSelector("textarea[id=newsBlock_Preview]").Clear();
            browser.FindElementByCssSelector("textarea[id=newsBlock_Preview]").SendKeys("Новые события");
            browser.FindElementByCssSelector("textarea[id=newsBlock_Body]").Clear();
            browser.FindElementByCssSelector("textarea[id=newsBlock_Body]").SendKeys("Новостной блок");
            browser.FindElementByCssSelector("input[id=newsBlock_Url]").Clear();
            browser.FindElementByCssSelector("input[id=newsBlock_Url]").SendKeys("Url-новости");
            browser.FindElementByCssSelector("input[id=newsBlock_IsPublished]").Click();
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Новость успешно отредактирована");
            DbSession.Refresh(newsBlock);
            Assert.That(newsBlock.Title, Is.StringContaining("Главная новость"), "Изменения в заглавии новостного блока должны измениться и в базе данных");
            Assert.That(newsBlock.Preview, Is.StringContaining("Новые события"), "Изменения в коротком описании новостного блока должны измениться и в базе данных");
            Assert.That(newsBlock.Body, Is.StringContaining("Новостной блок"), "Изменения в описании новостного блока должны измениться и в базе данных");
            Assert.That(newsBlock.Url, Is.StringContaining("Url-новости"), "Изменения в Url новостного блока должны измениться и в базе данных");
            Assert.That(newsBlock.IsPublished, Is.False, "Изменения в маркере опубликования новостного блока должны измениться и в базе данных");
        }
    }
}