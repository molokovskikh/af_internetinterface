using System.Linq;
using System.Web.WebPages;
using Common.Tools;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Rhino.Mocks;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class EditTVProtocolFixture : PlanFixture
	{

		[Test, Description("Изменение протокола для TV")]
		public void EditTVProtocol()
		{
			Open("Plans/TVProtocolList");
			var tVProtocol = DbSession.Query<TvProtocol>().First(p => p.Name == "rtp");
			var targetTVProtocolList = browser.FindElementByXPath("//td[contains(.,'" + tVProtocol.Name + "')]");
			var row = targetTVProtocolList.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			var tvChannelGroupName = browser.FindElementByCssSelector("input[id=TvProtocol_Name]");
			tvChannelGroupName.Clear();
			tvChannelGroupName.SendKeys("Изменен");
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("Протокол успешно изменен!");
			DbSession.Refresh(tVProtocol);
			Assert.That(tVProtocol.Name, Does.Contain("Изменен"), "Изменения протокола для TV должны сохраниться и в базе данных");
		}
	}
}