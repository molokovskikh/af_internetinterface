using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure.Helpers;
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
            var nameStart = "улица ленина";
            var nameNew = "советская улица";
            Open("Address/StreetList");
            var street = DbSession.Query<Street>().First(p => p.Name == nameStart);
            var targetStreet = browser.FindElementByXPath("//td[contains(.,'" + street.Name + "')]");
            var row = targetStreet.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector(".entypo-vcard"));
            button.Click();
            var streetEditName = browser.FindElementByCssSelector("input[id=Street_Name]");
            streetEditName.Clear();
            streetEditName.SendKeys(nameNew);
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Улица успешно изменена");
            DbSession.Refresh(street);
            Assert.That(street.Name, Is.StringContaining(nameNew),
                "Изменения должны сохраниться и в базе данных");
            Assert.That(street.PublicName(), Is.EqualTo(street.Name),
                "Изменения должны сохраниться и в базе данных");

            //редактирование псевдонима, сравнение
            Open("Address/StreetList");
            street = DbSession.Query<Street>().First(p => p.Name == nameNew);
            targetStreet = browser.FindElementByXPath("//td[contains(.,'" + street.Name + "')]");
            row = targetStreet.FindElement(By.XPath(".."));
            button = row.FindElement(By.CssSelector(".entypo-vcard"));
            button.Click();
            nameNew = "псевдо-советская улица";
            streetEditName = browser.FindElementByCssSelector("input[name='Street.Alias']");
            streetEditName.Clear();
            streetEditName.SendKeys(nameNew);
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Улица успешно изменена");
            DbSession.Refresh(street);
            Assert.That(street.PublicName(), Is.EqualTo(nameNew),
                "Изменения должны сохраниться и в базе данных");
        }

        [Test, Description("Изменение псевдонима улицы")]
        public void StreetAliasInCard()
        {
            var aliasNew = "NewAlias";
            var currentClient =
                DbSession.Query<Client>()
                    .FirstOrDefault(s => s.Comment == ClientCreateHelper.ClientMark.normalClient.GetDescription());

            Open($"Client/ConnectionCard/{currentClient.Id}");
            AssertNoText(aliasNew);
            Assert.That(currentClient.PhysicalClient.Address.House.Street.PublicName(),
                Is.EqualTo(currentClient.PhysicalClient.Address.House.Street.Name),
                "Изменения должны сохраниться и в базе данных");
            currentClient.PhysicalClient.Address.House.Street.Alias = aliasNew;
            DbSession.Save(currentClient.PhysicalClient.Address.House.Street);
            DbSession.Flush();
            Open($"Client/ConnectionCard/{currentClient.Id}");
            WaitForVisibleCss("div");
            AssertText(aliasNew);
            Open("Address/StreetList");
        }
    }
}