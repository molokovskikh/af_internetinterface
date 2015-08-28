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
    class EditBannerFixture : BannerFixture
    {
        [Test, Description("Изменение баннера")]
        public void DeleteBanner()
        {
            Inforoom2.Models.Banner banner = new Inforoom2.Models.Banner();
            banner.Url = "Url";
            banner.ImagePath = "/Images/";
            banner.LastEdit = DateTime.Now;
            banner.Enabled = true;
            banner.Partner = DbSession.Query<Employee>().FirstOrDefault();
            banner.Region = DbSession.Query<Region>().FirstOrDefault();
            DbSession.Save(banner);


            Open("Banner/BannerIndex");
            var bannerCount = DbSession.Query<Inforoom2.Models.Banner>().Count();
            var bannerEdit = DbSession.Query<Inforoom2.Models.Banner>().First(p => p.ImagePath == "/Images/");
            var targetBanner = browser.FindElementByXPath("//td[contains(.,'" + bannerEdit.Url + "')]");
            var row = targetBanner.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector(".entypo-vcard"));
            button.Click();
            browser.FindElementByCssSelector("input[id=banner_Url]").Clear();
            browser.FindElementByCssSelector("input[id=banner_Url]").SendKeys("Url-баннера");
            Css("#RegionDropDown").SelectByText("Белгород");
            browser.FindElementByCssSelector("input[id=isEnabled]").Click();
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Баннер успешно сохранен");

            DbSession.Refresh(bannerEdit);
            var bannerCountAfterTest = DbSession.Query<Inforoom2.Models.Banner>().Count();
            Assert.That(bannerCountAfterTest, Is.EqualTo(bannerCount), "Количество баннеров в базе данных должно остаться преждним");
            Assert.That(bannerEdit.Url, Is.EqualTo("Url-баннера"), "Url-баннера должно измениться и в базе данных");
            Assert.That(bannerEdit.Enabled, Is.False, "Метка о показе баннера должна измениться и в базе данных");
            Assert.That(bannerEdit.Region.Name, Is.EqualTo("Белгород"), "Регион у баннера должен измениться и в базе данных");
        }
    }
}
