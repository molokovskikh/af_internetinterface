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
    class DeleteNewsBlockFixture : NewsFixture
    {
        [Test, Description("Удаление новостного блока")]
        public void DeleteNewsBlock()
        {
            Open("News/NewsIndex");
            var newsBlock = DbSession.Query<NewsBlock>().First(p => p.Title == "Новость");
            var targetNewsBlock = browser.FindElementByXPath("//td[contains(.,'" + newsBlock.Title + "')]");
            var row = targetNewsBlock.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector(".entypo-cancel-circled"));
            button.Click();
            var deleteNewsBlock = DbSession.Query<NewsBlock>().FirstOrDefault(p => p.Title == newsBlock.Title);
            Assert.That(deleteNewsBlock, Is.Null, "Новостной блок должен удалиться в базе данных");
        }
    }
}