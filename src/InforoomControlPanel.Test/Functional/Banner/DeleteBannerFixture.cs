using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Banner
{
    class DeleteBannerFixture : BannerFixture
    {
        [Test, Description("Успешное удаление баннера")]
        public void DeleteBanner()
        {
            Open("Banner/BannerIndex");
            var bannerCount = DbSession.Query<Inforoom2.Models.Banner>().Count();
            var banner = DbSession.Query<Inforoom2.Models.Banner>().First(p => p.ImagePath == "/Images/actionfolk.png");
            var targetBanner = browser.FindElementByXPath("//td[contains(.,'" + banner.Url + "')]");
            var row = targetBanner.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Баннер успешно удален");
            //проверяем что в базе данных тоже удалился
            var bannerCountAfterTest = DbSession.Query<Inforoom2.Models.Banner>().Count();
            Assert.That(bannerCountAfterTest, Is.EqualTo(bannerCount - 1), "Баннер должен удалиться и в базе данных");
        }
    }
}
