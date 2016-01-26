using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Slide
{
    class CreateSlideFixture : SlideFixture
    {
        [Test, Description("Неуспешное создание слайда")]
        public void UnsuccessfulCreateSlide()
        {
            Open("Slide/SlideIndex");
            var slideCount = DbSession.Query<Inforoom2.Models.Slide>().Count();
            browser.FindElementByCssSelector(".SlideIndex .entypo-plus").Click();
            browser.FindElementByCssSelector("input[id=slide_Url]").SendKeys("Url-слайда");
            Css("#RegionDropDown").SelectByText("Белгород");
            browser.FindElementByCssSelector("input[id=isEnabled]").Click();
            browser.FindElementByCssSelector(".btn-green").Click();
            var slideCountAfterTest = DbSession.Query<Inforoom2.Models.Slide>().Count();
            Assert.That(slideCountAfterTest, Is.EqualTo(slideCount), "Количество слайдов в базе данных должно остаться преждним");
        }
    }
}
