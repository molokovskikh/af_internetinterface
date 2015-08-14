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
	class EditNetworkNode : SwitchFixture
	{
		[Test, Description("Изменение основных полей узла связи")]
		public void NetworkNodeEdit()
		{
			Open("Switch/NetworkNodeList");
			var networkNode = DbSession.Query<NetworkNode>().First(p => p.Name == "Узел связи по адресу Борисоглебск, улица ленина, 8");
			var targetNetworkNode = browser.FindElementByXPath("//td[contains(.,'" + networkNode.Name + "')]");
			var row = targetNetworkNode.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			browser.FindElementByCssSelector("input[id=NetworkNode_Name]").Clear();
			browser.FindElementByCssSelector("input[id=NetworkNode_Name]").SendKeys("Узел связи по измененному адресу");
			browser.FindElementByCssSelector("input[id=NetworkNode_Virtual]").Click();
			browser.FindElementByCssSelector("textarea[id=NetworkNode_Description]").SendKeys("Описание тестового узла связи");
			browser.FindElementByCssSelector(".btn-green.save").Click();
			AssertText("Узел связи успешно изменен");
			DbSession.Refresh(networkNode);
			Assert.That(networkNode.Name, Is.StringContaining("Узел связи по измененному адресу"), "Изменения имени узла связи должны сохраниться и в базе данных");
			Assert.That(networkNode.Virtual, Is.True, "Изменения маркера виртуальности должны сохраниться и в базе данных");
			Assert.That(networkNode.Description, Is.StringContaining("Описание тестового узла связи"), "Изменения описания узла связи должны сохраниться и в базе данных");
		}

		[Test, Description("Добавление многопарника узлу связи")]
		public void AddNetworkNodeTwistedPairs()
		{
			DbSession.Flush();
			Open("Switch/NetworkNodeList");
			var networkNode = DbSession.Query<NetworkNode>().First(p => p.Name == "Узел связи по адресу Борисоглебск, улица гагарина, 90");
			var targetNetworkNode = browser.FindElementByXPath("//td[contains(.,'" + networkNode.Name + "')]");
			var row = targetNetworkNode.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			Css("#TwistedPair_PairCount").SelectByText("16");
			browser.FindElementByCssSelector(".btn-green.add").Click();
			browser.FindElementByCssSelector(".btn-green.save").Click();
			DbSession.Refresh(networkNode);
			Assert.That(networkNode.TwistedPairs[0].PairCount, Is.EqualTo(16), "К узлу связи должен добавиться многопарник");
		}

		[Test, Description("Удаление адреса у узла связи")]
		public void DeleteNetworkNodeAddress()
		{
			Open("Switch/NetworkNodeList");
			var networkNode = DbSession.Query<NetworkNode>().First(p => p.Name == "Узел связи по адресу Борисоглебск, улица гагарина, 90");
			var targetNetworkNode = browser.FindElementByXPath("//td[contains(.,'" + networkNode.Name + "')]");
			var row = targetNetworkNode.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			var addressNetworkNode = browser.FindElementByXPath("//div[contains(.,'" + networkNode.Addresses[0].GetFullAddress() + "')]");
			var rowAddress = addressNetworkNode.FindElement(By.XPath(".."));
			var buttonTvChannel = rowAddress.FindElement(By.CssSelector(".entypo-cancel-circled"));
			buttonTvChannel.Click();
			AssertText("Адрес успешно удален");
			browser.FindElementByCssSelector(".btn-green.save").Click();
			var networkNodeAddress = DbSession.Query<SwitchAddress>().FirstOrDefault(p => p.NetworkNode.Id == networkNode.Id);
			Assert.That(networkNodeAddress, Is.Null, "Адрес у узла связи должен удалиться");
		}
	}
}
