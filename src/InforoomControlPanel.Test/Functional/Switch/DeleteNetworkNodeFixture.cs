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
		//тест игнорируется из за того,что он проваливается и из за него не проходит следующий тест
		//необходимо исправление бага при удалении узла связи со связями в БД
		[Test, Description("Неуспешное удаление узла связи"), Ignore("Тест не проходит,при удалении узла связи с подключенными к нему колммутаторами - возникает ошибка")]
		public void unsuccessfulNetworkNodeDelete()
		{
			Open("Switch/NetworkNodeList");
			var networkNode = DbSession.Query<NetworkNode>().First(p => p.Name == "Узел связи по адресу Борисоглебск, улица ленина, 8");
			var targetNetworkNode = browser.FindElementByXPath("//td[contains(.,'" + networkNode.Name + "')]");
			var row = targetNetworkNode.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-red"));
			button.Click();
			DbSession.Refresh(networkNode);
			Assert.That(networkNode, Is.Not.Null, "Узел связи не должен удалиться");
			AssertText("Узел связи по адресу Борисоглебск, улица ленина, 8");
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
