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
    class EditCityFixture : AddressFixture
    {
        [Test, Description("Изменение города")]
        public void CityEdit()
        {
            Open("Address/CityList");
            var city = DbSession.Query<City>().First(p => p.Name == "Белгород");
            var targetCity = browser.FindElementByXPath("//td[contains(.,'" + city.Name + "')]");
            var row = targetCity.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector(".entypo-vcard"));
            button.Click();
            var cityEdit = browser.FindElementByCssSelector("input[id=City_Name]");
            cityEdit.Clear();
            cityEdit.SendKeys("Москва");
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Город успешно изменен");
            DbSession.Refresh(city);
            Assert.That(city.Name, Is.StringContaining("Москва"), "Изменения  должны сохраниться и в базе данных");
        }
    }
}
