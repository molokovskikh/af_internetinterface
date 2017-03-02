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
    class EditSwitchAddressFixture : AddressFixture
    {
        [Test, Description("Изменение адреса коммутатора")]
        public void SwitchAddressEdit()
        {
            Open("Address/SwitchAddressList");
            var addressSwitch = DbSession.Query<SwitchAddress>().First(p => p.House.Street.Name == "улица гагарина");
            var targetAddress = browser.FindElementByXPath("//td[contains(.,'" + addressSwitch.House.Street.Name + "')]");
            var row = targetAddress.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-success"));
            button.Click();
            Css("#StreetProxyDropDown").SelectByText("улица ленина");
            Css("#HouseProxyDropDown").SelectByText("8");
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Адрес коммутатора успешно изменен");
            DbSession.Flush();
            DbSession.Refresh(addressSwitch);
            Assert.That(addressSwitch.House.Street.Name, Does.Contain("улица ленина"), "Изменения адреса коммутатора должны сохраниться и в базе данных");
        }
    }
}
