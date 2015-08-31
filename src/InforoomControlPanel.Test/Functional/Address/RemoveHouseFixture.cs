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
    class RemoveHouseFixture : AddressFixture
    {
        [Test, Description("Удаление дома,связанного с другими объектами")]
        public void unsuccessfulHouseDelete()
        {
            Open("Address/HouseList");
            var house = DbSession.Query<House>().First(p => p.Number == "1А");
            var targethouse = browser.FindElementByXPath("//td[contains(.,'" + house.Number + "')]");
            var row = targethouse.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Объект не удалось удалить! Возможно уже был связан с другими объектами.");
            //проверяем что в базе данных дом не удалился
            var deleteHouse = DbSession.Query<House>().FirstOrDefault(p => p.Id == house.Id);
            Assert.That(deleteHouse, Is.Not.Null, "Дом не должен удалиться в базе данных");
        }

        [Test, Description("Удаление дома,не связанного с другими объектами")]
        public void successfulHouseDelete()
        {
            var houseSave = DbSession.Query<House>().First(p => p.Number == "1А");
            var houseNew = new House();
            houseNew.Number = "134";
            houseNew.Geomark = "1234";
            houseNew.Street = houseSave.Street;
            houseNew.Region = houseSave.Region;
            DbSession.Save(houseNew);

            Open("Address/HouseList");
            var house = DbSession.Query<House>().First(p => p.Number == "134");
            var targethouse = browser.FindElementByXPath("//td[contains(.,'" + house.Number + "')]");
            var row = targethouse.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Объект успешно удален!");
            //проверяем что в базе данных дом  удалился
            var deleteHouse = DbSession.Query<House>().FirstOrDefault(p => p.Id == house.Id);
            Assert.That(deleteHouse, Is.Null, "Дом должен удалиться в базе данных");
        }
    }
}
