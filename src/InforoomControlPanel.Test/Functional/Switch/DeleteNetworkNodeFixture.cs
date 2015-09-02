using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Switch
{
	class DeleteNetworkNode : SwitchFixture
	{
		
		[Test, Description("Неуспешное удаление узла связи с привязанным коммутатором")]
		public void unsuccessfulNetworkNodeDeleteWithSwitch()
		{
			Open("Switch/NetworkNodeList");
			//Удаляем адреса и аренды, чтобы оставался только подключенный к узлу связи коммутатор
			var networkNode = DbSession.Query<NetworkNode>().First(p => p.Name == "Узел связи по адресу Борисоглебск, улица ленина, 8");
			var sw = networkNode.Switches.First();
			var leases = DbSession.Query<Lease>().Where(i => i.Switch == sw).ToList();
			leases.ForEach(i => DbSession.Delete(i));
			DbSession.Flush();
			var targetNetworkNode = browser.FindElementByXPath("//td[contains(.,'" + networkNode.Name + "')]");
			var row = targetNetworkNode.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-red"));
			button.Click();
			DbSession.Refresh(networkNode);
			Assert.That(networkNode, Is.Not.Null, "Узел связи не должен удалиться");
			AssertText("Узел связи по адресу Борисоглебск, улица ленина, 8");
			AssertText("Объект не удалось удалить");
		}

		[Test, Description("Успешное удаление узла связи")]
		public void successfulNetworkNodeDelete()
		{
			//Создаем узел связи без связей в БД
			var networkNode = new NetworkNode();
			networkNode.Name = "Тестовый узел без связей";
			DbSession.Save(networkNode);
			DbSession.Flush();

			Open("Switch/NetworkNodeList");		
			var targetNetworkNode = browser.FindElementByXPath("//td[contains(.,'" + networkNode.Name + "')]");
			var row = targetNetworkNode.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-red"));
			button.Click();
			AssertText("Узел связи успешно удален");
			var networkNodeEdit = DbSession.Query<NetworkNode>().FirstOrDefault(p => p.Name == "Тестовый узел без связей");
			Assert.That(networkNodeEdit, Is.Null, "Узел связи должен удалиться");
		}
	}
}
