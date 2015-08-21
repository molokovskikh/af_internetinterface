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
    class EditHouseFixture : AddressFixture
    {
        [Test, Description("Изменение дома")]
        public void HouseEdit()
        {
            Open("Address/HouseList");
            var house = DbSession.Query<House>().First(p => p.Number == "22");
            var targetHouse = browser.FindElementByXPath("//td[contains(.,'" + house.Number + "')]");
            var row = targetHouse.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector(".entypo-vcard"));
            button.Click();
            var houseEdit = browser.FindElementByCssSelector("input[id=House_Number]");
            houseEdit.Clear();
            houseEdit.SendKeys("77");
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Дом успешно изменен");
            DbSession.Refresh(house);
            Assert.That(house.Number, Is.StringContaining("77"), "Изменения  должны сохраниться и в базе данных");
        }
    }
}
