using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Address
{
    class CreateStreetFixture : AddressFixture
    {
        [Test, Description("Добавление новой улицы, подтвержденной картами яндекс")]
        public void StreetConfirmedYandexAdd()
        {
            Open("Address/StreetList");
            var streetCount = DbSession.Query<Street>().ToList().Count;
            browser.FindElementByCssSelector(".StreetList .entypo-plus").Click();
            WaitForMap();
            Css("#RegionDropDown").SelectByText("Борисоглебск");
            var streetName = browser.FindElementByCssSelector("input[id=Street_Name]");
            var streetYandex = browser.FindElementByCssSelector("input[id=yandexStreet]");
            var streetConfirmed = browser.FindElementByCssSelector("input[id=Street_Confirmed]");
            var streetYandexPosition = browser.FindElementByCssSelector("input[id=yandexPosition]");
            streetName.SendKeys("аэродромная улица");
            WaitForMap();
            streetConfirmed.Click();
            streetConfirmed.Click();
            var streetYandexCreate = streetYandex.GetAttribute("value");
            var streetYandexGeomark = streetYandexPosition.GetAttribute("value");
            WaitForMap();
            browser.FindElementByCssSelector(".btn.btn-green").Click();
            AssertText("Улица успешно добавлена");
            //Проверка, что после создания улицы количество улиц в базе данных увеличилось на один
            var streetCountAfterTest = DbSession.Query<Street>().ToList().Count;
            Assert.That(streetCountAfterTest, Is.EqualTo(streetCount + 1), "Количество улиц должно увеличиться на один после добавления новой улицы");
            //Проверяем, что в базе данных новая улица сохранилась с наименованием редактированным Яндексом
            var createStreet = DbSession.Query<Street>().First(p => p.Name == streetYandexCreate);
            Assert.That(createStreet.Name, Is.StringContaining(streetYandexCreate), "Добавленная улица должена сохраниться и в базе данных");
            //Проверяем, что в базе данных сохранились координаты созданные Яндексом
            Assert.That(createStreet.Geomark, Is.StringContaining(streetYandexGeomark), "У добавленной улицы должны быть сохранены координаты Яндекса");
            //Проверяем, что в базе данных стоит метка подтверждения улицы Яндексом
            Assert.That(createStreet.Confirmed, Is.True);
        }


        [Test, Description("Добавление новой улицы, не подтвержденной картами яндекс")]
        public void StreetNoConfirmedYandexAdd()
        {
            Open("Address/StreetList");
            browser.FindElementByCssSelector(".StreetList .entypo-plus").Click();
            WaitForMap();
            Css("#RegionDropDown").SelectByText("Борисоглебск");
            var streetName = browser.FindElementByCssSelector("input[id=Street_Name]");
            var streetConfirmed = browser.FindElementByCssSelector("input[id=Street_Confirmed]");
            var streetMetka = browser.FindElementByCssSelector("div[id=yandexMap]");
            var streetGeomark = browser.FindElementByCssSelector("input[id=Street_Geomark]");
            streetName.SendKeys("улица победы");
            WaitForMap();
            streetConfirmed.Click();
            streetMetka.Click();
            WaitForMap();
            streetMetka.Click();
            var streetGeomarkCreate = streetGeomark.GetAttribute("value");
            browser.FindElementByCssSelector(".btn.btn-green").Click();
            AssertText("Улица успешно добавлена");
            //Проверяем, что в базе данных новая улица сохранилась с наименованием, которое было написано тестом
            var createStreet = DbSession.Query<Street>().First(p => p.Name == "улица победы");
            Assert.That(createStreet.Name, Is.StringContaining("улица победы"), "Добавленная улица должена сохраниться и в базе данных");
            //Проверяем, что в базе данных сохранились координаты созданные тестом
            Assert.That(createStreet.Geomark, Is.StringContaining(streetGeomarkCreate), "У добавленной улицы должны быть сохранены координаты не подтвержденные Яндексом");
            //Проверяем, что в базе данных не стоит метка подтверждения улицы Яндексом
            Assert.That(createStreet.Confirmed, Is.False);
        }
    }
}
