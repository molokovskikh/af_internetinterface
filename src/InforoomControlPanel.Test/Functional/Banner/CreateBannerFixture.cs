using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Banner
{
    class CreateBannerFixture : BannerFixture
    {
        [Test, Description("Неуспешное создание баннера")]
        public void UnsuccessfulCreateBanner()
        {
            Open("Banner/BannerIndex");
            var bannerCount = DbSession.Query<Inforoom2.Models.Banner>().Count();
            browser.FindElementByCssSelector(".entypo-plus").Click();
            browser.FindElementByCssSelector("input[id=banner_Url]").SendKeys("Url-баннера");
            Css("#RegionDropDown").SelectByText("Белгород");
            browser.FindElementByCssSelector("input[id=isEnabled]").Click();
            browser.FindElementByCssSelector(".btn-green").Click();
            var bannerCountAfterTest = DbSession.Query<Inforoom2.Models.Banner>().Count();
            Assert.That(bannerCountAfterTest, Is.EqualTo(bannerCount), "Количество баннеров в базе данных должно остаться преждним");
        }
    }
}
