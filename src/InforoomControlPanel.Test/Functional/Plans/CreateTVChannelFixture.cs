using System.Linq;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class CreateTVChannelFixture : PlanFixture
	{
		[Test, Description("Добавление телевизионного канала")]
		public void CreateTVChannel()
		{
			Open("Plans/TVChannelList");
			browser.FindElementByCssSelector(".btn-green").Click();
			var tvChannelName = browser.FindElementByCssSelector("input[id=TvChannel_Name]");
			var tvChannelUrl = browser.FindElementByCssSelector("input[id=TvChannel_Url]");
			var tvChannelPort = browser.FindElementByCssSelector("input[id=TvChannel_Port]");
			tvChannelName.SendKeys("Тест");
			tvChannelUrl.SendKeys("12345");
			tvChannelPort.SendKeys("1213");
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("Канал успешно добавлен");
			var createTVChannel =  DbSession.Query<TvChannel>().First(p => p.Name == "Тест");
			Assert.That(createTVChannel.Name, Does.Contain("Тест"), "Добавленный канал должен сохраниться и в базе данных");

		}
		
	}
}