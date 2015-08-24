using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Address
{
    class CreateSwitchAddressFixture : AddressFixture
    {
        [Test, Description("Добавление нового адреса коммутатора")]
        public void SwitchAddressAdd()
        {
            Open("Address/SwitchAddressList");
            var addressCount = DbSession.Query<SwitchAddress>().ToList().Count;
            browser.FindElementByCssSelector(".entypo-plus").Click();
            Css("#RegionDropDown").SelectByText("Борисоглебск");
            Css("#StreetDropDown").SelectByText("улица третьяковская");
            Css("#HouseDropDown").SelectByText("6Б");
            Css("#NetworkNodeDropDown").SelectByText("Узел связи по адресу Борисоглебск, улица третьяковская, 6Б");
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Адрес коммутатора успешно добавлен");
            var addressCountAfterTest = DbSession.Query<SwitchAddress>().ToList().Count;
            Assert.That(addressCountAfterTest, Is.EqualTo(addressCount + 1), "Количество адресов коммутаторов должен увеличиться на один при добавлении нового");
            var address = DbSession.Query<SwitchAddress>().ToList().OrderByDescending(p => p.Id).First();
            Assert.That(address.House.Number, Is.StringContaining("6Б"), "В базе данных у нового созданного адреса должен сохраниться дом,который был указан при создании");
            Assert.That(address.House.Street.Name, Is.StringContaining("улица третьяковская"), "В базе данных у нового созданного адреса должена сохраниться улица,которая была указана при создании");
            Assert.That(address.NetworkNode.Name, Is.StringContaining("Узел связи по адресу Борисоглебск, улица третьяковская, 6Б"), "В базе данных у нового созданного адреса должен сохраниться узел связи,который был указан при создании");
        }
    }
}
