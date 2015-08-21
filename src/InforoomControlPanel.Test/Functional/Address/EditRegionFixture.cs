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
    class EditRegionFixture : AddressFixture
    {
        [Test, Description("Изменение региона")]
        public void RegionEdit()
        {
            Open("Address/RegionList");
            var region = DbSession.Query<Region>().First(p => p.Name == "Белгород");
            var targetRegion = browser.FindElementByXPath("//td[contains(.,'" + region.Name + "')]");
            var row = targetRegion.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector(".entypo-vcard"));
            button.Click();
            browser.FindElementByCssSelector("input[id=Region_Name]").SendKeys(" изменен");
            Css("#CityProxyDropDown").SelectByText("Борисоглебск");
            browser.FindElementByCssSelector("input[id=Region_OfficeAddress]").Clear();
            browser.FindElementByCssSelector("input[id=Region_OfficeAddress]").SendKeys("изменен");
            browser.FindElementByCssSelector("input[id=Region_RegionOfficePhoneNumber]").Clear();
            browser.FindElementByCssSelector("input[id=Region_RegionOfficePhoneNumber]").SendKeys("1234");
            browser.FindElementByCssSelector("input[id=Region_OfficeGeomark]").Clear();
            browser.FindElementByCssSelector("input[id=Region_OfficeGeomark]").SendKeys("1234");
            browser.FindElementByCssSelector("input[id=Region_ShownOnMainPage]").Click();
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Регион успешно изменен");
            DbSession.Refresh(region);
            Assert.That(region.Name, Is.StringContaining("Белгород изменен"), "Изменения в имени  должны измениться и в базе данных");
            Assert.That(region.City.Name, Is.StringContaining("Борисоглебск"), "Изменения в городе  должны измениться и в базе данных");
            Assert.That(region.OfficeAddress, Is.StringContaining("изменен"), "Изменения в адресе офиса  должны измениться и в базе данных");
            Assert.That(region.RegionOfficePhoneNumber, Is.StringContaining("1234"), "Изменения в номере телефона офиса  должны измениться и в базе данных");
            Assert.That(region.OfficeGeomark, Is.StringContaining("1234"), "Изменения в геометке офиса  должны измениться и в базе данных");
        }
    }
}
