using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Address
{
    class RemoveRegionFixture : AddressFixture
    {
        [Test, Description("Удаление региона,связанного с другими объектами")]
        public void unsuccessfulRegionDelete()
        {
            Open("Address/RegionList");
            var region = DbSession.Query<Region>().First(p => p.Name == "Белгород");
            var targetRegion = browser.FindElementByXPath("//td[contains(.,'" + region.Name + "')]");
            var row = targetRegion.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Объект не удалось удалить");
            //проверяем что в базе данных регион не удалился
            var deleteRegion = DbSession.Query<Region>().FirstOrDefault(p => p.Id == region.Id);
            Assert.That(deleteRegion, Is.Not.Null, "Регион не должен удалиться в базе данных");
        }

        [Test, Description("Удаление региона,не связанного с другими объектами")]
        public void successfulRegionDelete()
        {
            var regionSave = DbSession.Query<Region>().First(p => p.Name == "Белгород");
            var regionNew = new Region();
            regionNew.Name = "Воронеж";
            regionNew.OfficeGeomark = "1234";
            regionNew.City = regionSave.City;
            DbSession.Save(regionNew);

            Open("Address/RegionList");
            var region = DbSession.Query<Region>().First(p => p.Name == "Воронеж");
            var targetRegion = browser.FindElementByXPath("//td[contains(.,'" + region.Name + "')]");
            var row = targetRegion.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Объект успешно удален!");
            //проверяем что в базе данных регион  удалился
            var deleteRegion = DbSession.Query<Region>().FirstOrDefault(p => p.Id == region.Id);
            Assert.That(deleteRegion, Is.Null, "Регион должен удалиться в базе данных");
        }
    }
}
