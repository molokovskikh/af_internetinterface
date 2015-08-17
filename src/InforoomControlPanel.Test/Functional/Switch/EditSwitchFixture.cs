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
	class EditSwitchFixture : SwitchFixture
	{
		[Test, Description("Изменение коммутатора")]
		public void EditSwitch()
		{
			Open("Switch/SwitchList");
			var switchEdit = DbSession.Query<Inforoom2.Models.Switch>().First(p => p.Name == "Тестовый коммутатор по адресу Борисоглебск. улица ленина. 8");
			var targetSwitchEdit = browser.FindElementByXPath("//td[contains(.,'" + switchEdit.Name + "')]");
			var row = targetSwitchEdit.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			browser.FindElementByCssSelector("input[id=Switch_Name]").Clear();
			browser.FindElementByCssSelector("input[id=Switch_Name]").SendKeys("Коммутатор для тестирования");
			browser.FindElementByCssSelector("input[id=Switch_PortCount]").Clear();
			browser.FindElementByCssSelector("input[id=Switch_PortCount]").SendKeys("10");
			Css("#NetworkNodeDropDown").SelectByText("Тестовый узел");
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("Коммутатор успешно изменнен");
			DbSession.Refresh(switchEdit);
			Assert.That(switchEdit.Name, Is.StringContaining("Коммутатор для тестирования"), "Изменения наименования коммутатора должны сохраниться и в базе данных");
			Assert.That(switchEdit.PortCount, Is.EqualTo(10), "Изменения колличества портов коммутатора коммутатора должны сохраниться и в базе данных");
			Assert.That(switchEdit.NetworkNode.Name, Is.StringContaining("Тестовый узел"), "Изменения наименования узла связи коммутатора коммутатора должны сохраниться и в базе данных");
			
		}
	}
}
