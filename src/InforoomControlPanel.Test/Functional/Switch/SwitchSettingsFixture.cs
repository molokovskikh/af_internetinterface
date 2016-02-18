using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Zone = Inforoom2.Models.Zone;

namespace InforoomControlPanel.Test.Functional.Switch
{
	class SwitchSettingsFixture : SwitchFixture
	{
		[Test, Description("Изменение коммутатора")]
		public void CreateSwitch()
		{
			Open("Switch/SwitchCreate");
			var name = "Коммутатор для тестирования 2000";
			var switchIp = "105.168.11.1";
			var switchMac = "EC-FE-C5-36-1A-27";
			var switchPortCount = "12";
			var switchDescription = "Такое себе описание";
			var netWorkNode = DbSession.Query<NetworkNode>().ToList()[2];
			var zone = DbSession.Query<Zone>().FirstOrDefault(s => s.Name.IndexOf("Белгород") != -1);
			var switchType = Inforoom2.Models.SwitchType.Dlink;

			var inputObj = browser.FindElementByCssSelector("input[name='Switch.Name']");
			inputObj.Clear();
			inputObj.SendKeys(name);
			inputObj = browser.FindElementByCssSelector("input[name='switchIp']");
			inputObj.Clear();
			inputObj.SendKeys(switchIp);
			inputObj = browser.FindElementByCssSelector("input[name='Switch.Mac']");
			inputObj.Clear();
			inputObj.SendKeys(switchMac);
			inputObj = browser.FindElementByCssSelector("input[name='Switch.PortCount']");
			inputObj.Clear();
			inputObj.SendKeys(switchPortCount);
			inputObj = browser.FindElementByCssSelector("textarea[name='Switch.Description']");
			inputObj.Clear();
			inputObj.SendKeys(switchDescription);
			Css( "[name='Switch.NetworkNode.Id']").SelectByText(netWorkNode.Name);
			Css("[name='Switch.Zone.Id']").SelectByText(zone.Name);
			Css("[name='Switch.Type']").SelectByText(switchType.GetDescription());
			browser.FindElementByCssSelector(".btn-green").Click();

			AssertText("Коммутатор успешно добавлен");
			var switchEdit = DbSession.Query<Inforoom2.Models.Switch>().FirstOrDefault(s => s.Name == name);

            Assert.That(switchEdit.Name, Is.EqualTo(name), "Неверно задано значение: Наименование");
			Assert.That(switchEdit.Ip.ToString(), Is.EqualTo(switchIp), "Неверно задано значение: Ip");
			Assert.That(switchEdit.Mac, Is.EqualTo(switchMac), "Неверно задано значение: Mac");
			Assert.That(switchEdit.PortCount.ToString(), Is.EqualTo(switchPortCount), "Неверно задано значение: Кол-во портов");
			Assert.That(switchEdit.Description, Is.EqualTo(switchDescription), "Неверно задано значение: Описание");
			Assert.That(switchEdit.NetworkNode.Id, Is.EqualTo(netWorkNode.Id), "Неверно задано значение: Узел связи");
			Assert.That(switchEdit.Zone.Id, Is.EqualTo(zone.Id), "Неверно задано значение: Зона");
			Assert.That(switchEdit.Type, Is.EqualTo(switchType), "Неверно задано значение: Тип коммутатора");

		}
		[Test, Description("Изменение коммутатора")]
		public void EditSwitch()
		{
			Open("Switch/SwitchList");
			var switchEdit = DbSession.Query<Inforoom2.Models.Switch>().First(p => p.Name == "Тестовый коммутатор по адресу г. Борисоглебск, ул. Ленина, д. 8, подъезд 1, этаж 1");
			var targetSwitchEdit = browser.FindElementByXPath("//td[contains(.,'" + switchEdit.Name + "')]");
			var row = targetSwitchEdit.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			ClosePreviousTab();


			var name = "Коммутатор для тестирования 2000";
			var switchIp = "105.111.11.1";
			var switchMac = "EC-FE-99-36-1A-99";
			var switchPortCount = "102";
			var switchDescription = "Такое себе описание 1234567890";
			var netWorkNode = DbSession.Query<NetworkNode>().ToList()[2];
			var zone = DbSession.Query<Zone>().FirstOrDefault(s => s.Name.IndexOf("Белгород") != -1);
			var switchType = Inforoom2.Models.SwitchType.Dlink;

			var inputObj = browser.FindElementByCssSelector("input[name='Switch.Name']");
			inputObj.Clear();
			inputObj.SendKeys(name);
			inputObj = browser.FindElementByCssSelector("input[name='switchIp']");
			inputObj.Clear();
			inputObj.SendKeys(switchIp);
			browser.FindElementByCssSelector(".col-sm-3.control-label").Click();
			inputObj = browser.FindElementByCssSelector("input[name='Switch.Mac']");
			inputObj.Clear();
			inputObj.SendKeys(switchMac);
			inputObj = browser.FindElementByCssSelector("input[name='Switch.PortCount']");
			inputObj.Clear();
			inputObj.SendKeys(switchPortCount);
			inputObj = browser.FindElementByCssSelector("textarea[name='Switch.Description']");
			inputObj.Clear();
			inputObj.SendKeys(switchDescription);
			Css("[name='Switch.NetworkNode.Id']").SelectByText(netWorkNode.Name);
			Css("[name='Switch.Zone.Id']").SelectByText(zone.Name);
			Css("[name='Switch.Type']").SelectByText(switchType.GetDescription());
			
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("Коммутатор успешно изменнен");
			DbSession.Refresh(switchEdit);

			Assert.That(switchEdit.Name, Is.EqualTo(name), "Неверно задано значение: Наименование");
			Assert.That(switchEdit.Ip.ToString(), Is.EqualTo(switchIp), "Неверно задано значение: Ip");
			Assert.That(switchEdit.Mac, Is.EqualTo(switchMac), "Неверно задано значение: Mac");
			Assert.That(switchEdit.PortCount.ToString(), Is.EqualTo(switchPortCount), "Неверно задано значение: Кол-во портов");
			Assert.That(switchEdit.Description, Is.EqualTo(switchDescription), "Неверно задано значение: Описание");
			Assert.That(switchEdit.NetworkNode.Id, Is.EqualTo(netWorkNode.Id), "Неверно задано значение: Узел связи");
			Assert.That(switchEdit.Zone.Id, Is.EqualTo(zone.Id), "Неверно задано значение: Зона");
			Assert.That(switchEdit.Type, Is.EqualTo(switchType), "Неверно задано значение: Тип коммутатора");

		}
	}
}
