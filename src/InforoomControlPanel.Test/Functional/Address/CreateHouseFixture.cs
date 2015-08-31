using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Address
{
    class CreateHouseFixture : AddressFixture
    {
        [Test, Description("Добавление нового дома, подтвержденного картами яндекс")]
        public void HouseConfirmedYandexAdd()
        {
            Open("Address/HouseList");
            var houseCount = DbSession.Query<House>().ToList().Count;
            browser.FindElementByCssSelector(".entypo-plus").Click();
            Css("#StreetDropDown").SelectByText("улица третьяковская");
            Css("#RegionDropDown").SelectByText("Борисоглебск");
            WaitForMap();
            var houseNumber = browser.FindElementByCssSelector("input[id=House_Number]");
            var houseConfirmed = browser.FindElementByCssSelector("input[id=House_Confirmed]");
            var houseYandex = browser.FindElementByCssSelector("input[id=yandexHouse]");
            var houseYandexPosition = browser.FindElementByCssSelector("input[id=yandexPosition]");
            //Асерт, что выбран houseConfirmed
            houseNumber.SendKeys("16");
            WaitForMap();
            var houseYandexCreate = houseYandex.GetAttribute("value");
            var houseYandexGeomark = houseYandexPosition.GetAttribute("value");      
            browser.FindElementByCssSelector(".btn.btn-green").Click();
            AssertText("Дом успешно добавлен");
            //Проверка, что после создания дома количество домов в базе данных увеличилось на один
            var houseCountAfterTest = DbSession.Query<House>().ToList().Count;
            Assert.That(houseCountAfterTest, Is.EqualTo(houseCount + 1), "Количество домов должно увеличиться на один после добавления нового дома");
            //Проверяем, что в базе данных новый дом сохранился с наименованием редактированным Яндексом
            var createHouse = DbSession.Query<House>().First(p => p.Number == houseYandexCreate);
            Assert.That(createHouse.Number, Is.StringContaining(houseYandexCreate), "Добавленный дом должен сохраниться и в базе данных");
            //Проверяем, что в базе данных сохранились координаты созданные Яндексом
            Assert.That(createHouse.Geomark, Is.StringContaining(houseYandexGeomark), "У добавленного дома должны быть сохранены координаты Яндекса");
            //Проверяем, что в базе данных стоит метка подтверждения улицы Яндексом
            Assert.That(createHouse.Confirmed, Is.True);
        }

        [Test, Description("Добавление нового дома, не подтвержденного картами яндекс")]
        public void HouseNoConfirmedYandexAdd()
        {
            Open("Address/HouseList");
            browser.FindElementByCssSelector(".entypo-plus").Click();
            WaitForMap();
            Css("#StreetDropDown").SelectByText("улица третьяковская");
            var houseNumber = browser.FindElementByCssSelector("input[id=House_Number]");
            var houseConfirmed = browser.FindElementByCssSelector("input[id=House_Confirmed]");
            var houseMetka = browser.FindElementByCssSelector("div[id=yandexMap]");
            var houseGeomark = browser.FindElementByCssSelector("input[id=House_Geomark]");
            houseNumber.SendKeys("7");
            houseConfirmed.Click();
            houseMetka.Click();
            var houseGeomarkCreate = houseGeomark.GetAttribute("value");
            browser.FindElementByCssSelector(".btn.btn-green").Click();
            AssertText("Дом успешно добавлен");
            //Проверяем, что в базе данных новый дом сохранился с наименованием, которое было написано тестом
            var createHouse = DbSession.Query<House>().First(p => p.Number == "7");
            Assert.That(createHouse.Number, Is.StringContaining("7"), "Добавленный дом должен сохраниться и в базе данных");
            //Проверяем, что в базе данных сохранились координаты созданные тестом
            Assert.That(createHouse.Geomark, Is.StringContaining(houseGeomarkCreate), "У добавленного дома должны быть сохранены координаты не подтвержденные Яндексом");
            //Проверяем, что в базе данных не стоит метка подтверждения улицы Яндексом
            Assert.That(createHouse.Confirmed, Is.False);
        }
    }
}
