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
    class EditStreetFixture : AddressFixture
    {
        [Test, Description("Изменение улицы")]
        public void StreetEdit()
        {
            Open("Address/StreetList");
            var street = DbSession.Query<Street>().First(p => p.Name == "улица ленина");
            var targetStreet = browser.FindElementByXPath("//td[contains(.,'" + street.Name + "')]");
            var row = targetStreet.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector(".entypo-vcard"));
            button.Click();
            var streetEditName = browser.FindElementByCssSelector("input[id=Street_Name]");
            streetEditName.Clear();
            streetEditName.SendKeys("советская улица");
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Улица успешно изменена");
            DbSession.Refresh(street);
            Assert.That(street.Name, Is.StringContaining("советская улица"), "Изменения  должны сохраниться и в базе данных");
        }
    }
}
