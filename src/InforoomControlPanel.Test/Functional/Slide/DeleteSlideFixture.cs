using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Slide
{
    class DeleteSlideFixture : SlideFixture
    {
        [Test, Description("Успешное удаление слайда")]
        public void DeleteSlide()
        {
            Open("Slide/SlideIndex");
            var slideCount = DbSession.Query<Inforoom2.Models.Slide>().Count();
            var slide = DbSession.Query<Inforoom2.Models.Slide>().First();
            var targetSlide = browser.FindElementByXPath("//td[contains(.,'" + slide.Url + "')]");
            var row = targetSlide.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Слайд успешно удален");
            //проверяем что в базе данных тоже удалился
            var slideCountAfterTest = DbSession.Query<Inforoom2.Models.Slide>().Count();
            Assert.That(slideCountAfterTest, Is.EqualTo(slideCount - 1), "Слайд должен удалиться и в базе данных");
        }
    }
}
