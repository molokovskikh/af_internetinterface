using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Address
{
    class CreateRegionFixture : AddressFixture
    {
        [Test, Description("Добавление нового региона")]
        public void CreateRegion()
        {
            Open("Address/RegionList");
            browser.FindElementByCssSelector(".RegionList .entypo-plus").Click();
            browser.FindElementByCssSelector("input[id=Region_Name]").SendKeys("Ярославль");
            Css("#CityDropDown").SelectByText("Белгород");
            browser.FindElementByCssSelector("input[id=Region_OfficeAddress]").SendKeys("улица Трубецкого д12");
            browser.FindElementByCssSelector("input[id=Region_RegionOfficePhoneNumber]").SendKeys("88002000800");
            browser.FindElementByCssSelector("input[id=Region_OfficeGeomark]").SendKeys("50.59.5969998");
            browser.FindElementByCssSelector("input[id=Region_ShownOnMainPage]").Click();
            Css("#RegionDropDown").SelectByText("Белгород");
            browser.FindElementByCssSelector(".btn.btn-green").Click();
            AssertText("Регион успешно добавлен");
            var createRegion = DbSession.Query<Region>().First(p => p.Name == "Ярославль");
            Assert.That(createRegion, Is.Not.Null, "Регион должен сохраниться в базе данных");
        }
    }
}
