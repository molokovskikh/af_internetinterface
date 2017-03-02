using System.Linq;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class EditTVChannelFixture : PlanFixture
	{
		[Test, Description("Изменение телевизионного канала")]
		public void EditTVChannel()
		{
			Open("Plans/TVChannelList");
			var tvChannel = DbSession.Query<TvChannel>().First(p => p.Name == "ТНТ");
			var targetTvChannel = browser.FindElementByXPath("//td[contains(.,'" + tvChannel.Name + "')]");
			var row = targetTvChannel.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			var tvChannelName = browser.FindElementByCssSelector("input[id=TvChannel_Name]");
			tvChannelName.Clear();
			tvChannelName.SendKeys("Изменен");
			browser.FindElementByCssSelector("input[id=TvChannel_Enabled]").Click();
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("Канал успешно изменен");
			DbSession.Refresh(tvChannel);
			Assert.That(tvChannel.Name, Does.Contain("Изменен"), "Изменения канала должны сохраниться и в базе данных");
		}
		
	}
}