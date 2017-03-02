using System.Linq;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class CreateTVProtocolFixture : PlanFixture
	{
		[Test, Description("Добавление TV-протокола")]
		public void CreateTVChannelGroup()
		{
			Open("Plans/TvProtocolList");
			var tVProtocolCount = DbSession.Query<TvProtocol>().ToList().Count;
			browser.FindElementByCssSelector(".btn-green").Click();
			var tvProtocolName = browser.FindElementByCssSelector("input[id=TvProtocol_Name]");
			tvProtocolName.SendKeys("Тест");
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("Протокол успешно добавлен!");
			var createTVProtocol = DbSession.Query<TvProtocol>().First(p => p.Name == "Тест");
			Assert.That(createTVProtocol.Name, Does.Contain("Тест"), "Добавленный канал должен сохраниться и в базе данных");
			var tVProtocolCountAfterTest = DbSession.Query<TvProtocol>().ToList().Count;
			Assert.That(tVProtocolCountAfterTest, Is.EqualTo(tVProtocolCount + 1), "Количество TV-протоколов должно увеличиться на один после добавления");
		}	
	}
}