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
    class RemoveStreetFixture : AddressFixture
    {
        [Test, Description("Удаление улицы,связанную с другими объектами")]
        public void unsuccessfulStreetDelete()
        {
            Open("Address/StreetList");
            var street = DbSession.Query<Street>().First(p => p.Name == "улица ленина");
            var targetStreet = browser.FindElementByXPath("//td[contains(.,'" + street.Name + "')]");
            var row = targetStreet.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Объект не удалось удалить! Возможно уже был связан с другими объектами.");
            //проверяем что в базе данных улица не удалилась
            var deleteStreet = DbSession.Query<Street>().FirstOrDefault(p => p.Id == street.Id);
            Assert.That(deleteStreet, Is.Not.Null, "Улица не должена удалиться в базе данных");
        }

        [Test, Description("Удаление улицы,не связанную с другими объектами")]
        public void successfulStreetDelete()
        {
            var streetSave = DbSession.Query<Street>().First(p => p.Name == "улица ленина");
            var streetNew = new Street();
            streetNew.Name = "улица солнечная";
            streetNew.Geomark = "1234";
            streetNew.Region = streetSave.Region;
            DbSession.Save(streetNew);

            Open("Address/StreetList");
            var street = DbSession.Query<Street>().First(p => p.Name == "улица солнечная");
            var targetStreet = browser.FindElementByXPath("//td[contains(.,'" + street.Name + "')]");
            var row = targetStreet.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Объект успешно удален!");
            //проверяем что в базе данных улица  удалилась
            var deleteStreet = DbSession.Query<Street>().FirstOrDefault(p => p.Id == street.Id);
            Assert.That(deleteStreet, Is.Null, "Улица должена удалиться в базе данных");
        }
    }
}
