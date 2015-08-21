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
    class RemoveCityFixture : AddressFixture
    {
        [Test, Description("Удаление города,связанного с другими объектами")]
        public void unsuccessfulCityDelete()
        {
            Open("Address/CityList");
            var city = DbSession.Query<City>().First(p => p.Name == "Белгород");
            var targetCity = browser.FindElementByXPath("//td[contains(.,'" + city.Name + "')]");
            var row = targetCity.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Объект не удалось удалить! Возможно уже был связан с другими объектами.");
            //проверяем что в базе данных город не удалился
            var deleteCity = DbSession.Query<City>().FirstOrDefault(p => p.Id == city.Id);
            Assert.That(deleteCity, Is.Not.Null, "Город не должен удалиться в базе данных");
        }

        [Test, Description("Удаление города,не связанного с другими объектами")]
        public void successfulCityDelete()
        {
            var cityNew = new City();
            cityNew.Name = "Москва";
            DbSession.Save(cityNew);

            Open("Address/CityList");
            var city = DbSession.Query<City>().First(p => p.Name == "Москва");
            var targetCity = browser.FindElementByXPath("//td[contains(.,'" + city.Name + "')]");
            var row = targetCity.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Объект успешно удален!");
            //проверяем что в базе данных город  удалился
            var deleteCity = DbSession.Query<City>().FirstOrDefault(p => p.Id == city.Id);
            Assert.That(deleteCity, Is.Null, "Город должен удалиться в базе данных");
        }
    }
}
