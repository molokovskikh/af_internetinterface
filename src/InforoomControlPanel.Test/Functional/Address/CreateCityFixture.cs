using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Address
{
    class CreateCityFixture : AddressFixture
    {
        [Test, Description("Добавление нового города")]
        public void CreateCity()
        {
            Open("Address/CityList");
            browser.FindElementByCssSelector(".CityList .entypo-plus").Click();
            var cityName = browser.FindElementByCssSelector("input[class=form-control]");
            cityName.SendKeys("Москва");
            browser.FindElementByCssSelector(".btn.btn-green").Click();
            AssertText("Город успешно добавлен");
            var createCity = DbSession.Query<City>().First(p => p.Name == "Москва");
            Assert.That(createCity, Is.Not.Null, "Город должен сохраниться в базе данных");
        }
    }
}
