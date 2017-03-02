using System.Linq;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class DeleteTVProtocolFixture : PlanFixture
	{
		[Test, Description("Проверка успешного удаления протокола для TV (при условии,что он не прикреплен к TV-каналам)")]
		public void SuccessfulEditTVProtocol()
		{
			Open("Plans/TvProtocolList");
			var tvProtocol = DbSession.Query<TvProtocol>().First(p => p.Name == "test");
			var targetTvProtocol = browser.FindElementByXPath("//td[contains(.,'" + tvProtocol.Name + "')]");
			var row = targetTvProtocol.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-red"));
			button.Click();
			AssertText("Объект успешно удален");
			//проверяем что в базе данных тоже удалился
			var successfulEditTvProtocol = DbSession.Query<TvProtocol>().FirstOrDefault(p => p.Name == "test");
			Assert.That(successfulEditTvProtocol, Is.EqualTo(null), "Группа каналов должена удалиться и в базе данных");

		}

		[Test, Description("Проверка неудачного удаления протокола для TV (при условии что он прикреплен к TV-каналам)")]
		public void UnsuccessfulEditTVProtocol()
		{
			Open("Plans/TvProtocolList");
			var targetTvChannelGroup = browser.FindElementByXPath("//td[contains(.,'udp')]");
			var row = targetTvChannelGroup.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-red"));
			button.Click();
			AssertText("Объект не удалось удалить! Возможно уже был связан с другими объектами");
			var UnsuccessfulEditTvProtocol = DbSession.Query<TvProtocol>().FirstOrDefault(p => p.Name == "udp");
			Assert.That(UnsuccessfulEditTvProtocol.Name, Does.Contain("udp"), "Протокол для TV должен все еще быть сохранен в базе данных");
		}
	}
}