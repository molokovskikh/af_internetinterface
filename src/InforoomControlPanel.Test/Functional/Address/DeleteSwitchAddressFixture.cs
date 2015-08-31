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
    class DeleteSwitchAddressFixture : AddressFixture
    {
        [Test, Description("Удаление адреса коммутатора")]
        public void SwitchAddressDelete()
        {
            Open("Address/SwitchAddressList");
            var addressSwitch = DbSession.Query<SwitchAddress>().First(p => p.House.Street.Name == "улица третьяковская");
            var targetSwitchAddress = browser.FindElementByXPath("//td[contains(.,'" + addressSwitch.House.Street.Name + "')]");
            var row = targetSwitchAddress.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Адрес успешно удален");
            //проверяем что в базе данных тоже удалился
            var successfulEditAddressSwitch = DbSession.Query<SwitchAddress>().FirstOrDefault(p => p.House.Street.Name == "улица третьяковская");
            Assert.That(successfulEditAddressSwitch, Is.EqualTo(null), "Адрес коммутатора должен удалиться и в базе данных");

        }
    }
}
