using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Switch
{
	class CreateNetworkNode : SwitchFixture
	{
		[Test, Description("Добавление узла связи")]
		public void CreateNetworkNodeFixture()
		{
			Open("Switch/NetworkNodeList");
			var button = browser.FindElement(By.CssSelector(".NetworkNodeList i.entypo-plus"));
			button.Click();
			browser.FindElementByCssSelector("input[id=NetworkNode_Name]").SendKeys("Узел связи для тестирования");
			browser.FindElementByCssSelector("input[id=NetworkNode_Virtual]").Click();
			browser.FindElementByCssSelector("textarea[id=NetworkNode_Description]").SendKeys("Описание тестового узла связи");
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("Узел связи успешно добавлен");
			var networkNode = DbSession.Query<NetworkNode>().First(p => p.Name == "Узел связи для тестирования");
			Assert.That(networkNode.Name, Is.StringContaining("Узел связи для тестирования"), "Узел связи должен добавиться и в базе данных");
			Assert.That(networkNode.Virtual, Is.True, "Маркер виртуальности у узла связи должно сохраниться корректно");
			Assert.That(networkNode.Description, Is.StringContaining("Описание тестового узла связи"), "Описание узла связи при добавлении должно сохраниться корректно");	
		}
	}
}
