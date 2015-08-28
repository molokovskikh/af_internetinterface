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
    class EditSlideFixture : SlideFixture
    {
        [Test, Description("Изменение слайда")]
        public void DeleteBanner()
        {
            Inforoom2.Models.Slide slide = new Inforoom2.Models.Slide();
            slide.Url = "Url";
            slide.ImagePath = "/Images/";
            slide.LastEdit = DateTime.Now;
            slide.Enabled = true;
            slide.Partner = DbSession.Query<Employee>().FirstOrDefault();
            slide.Region = DbSession.Query<Region>().FirstOrDefault();
            DbSession.Save(slide);
            DbSession.Flush();

            Open("Slide/SlideIndex");
            var slideCount = DbSession.Query<Inforoom2.Models.Slide>().Count();
            var slideEdit = DbSession.Query<Inforoom2.Models.Slide>().First(p => p.ImagePath == "/Images/");
            var targetSlide = browser.FindElementByXPath("//td[contains(.,'" + slideEdit.Url + "')]");
            var row = targetSlide.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector(".entypo-vcard"));
            button.Click();
            browser.FindElementByCssSelector("input[id=slide_Url]").Clear();
            browser.FindElementByCssSelector("input[id=slide_Url]").SendKeys("Url-slide");
            Css("#RegionDropDown").SelectByText("Белгород");
            browser.FindElementByCssSelector("input[id=isEnabled]").Click();
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Слайд успешно сохранен");

            DbSession.Refresh(slideEdit);
            var slideCountAfterTest = DbSession.Query<Inforoom2.Models.Slide>().Count();
            Assert.That(slideCountAfterTest, Is.EqualTo(slideCount), "Количество слайдов в базе данных должно остаться преждним");
            Assert.That(slideEdit.Url, Is.EqualTo("Url-slide"), "Url-слайда должно измениться и в базе данных");
            Assert.That(slideEdit.Enabled, Is.False, "Метка о показе слайда должна измениться и в базе данных");
            Assert.That(slideEdit.Region.Name, Is.EqualTo("Белгород"), "Регион у слайда должен измениться и в базе данных");
        }
    }
}
