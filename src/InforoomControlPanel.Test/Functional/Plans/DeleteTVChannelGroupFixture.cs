using System.Linq;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class DeleteTVChannelGroupFixture : PlanFixture
	{
		[Test, Description("Проверка успешного удаления группы TV-каналов (при условии,что она не прикреплена к каналам)")]
		public void SuccessfulEditTVChannelGroup()
		{
			Open("Plans/TVChannelGroupList");
			var tvChannelGroup = DbSession.Query<TvChannelGroup>().First(p => p.Name == "Детская");
			var targetTvChannelGroup = browser.FindElementByXPath("//td[contains(.,'" + tvChannelGroup.Name + "')]");
			var row = targetTvChannelGroup.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-red"));
			button.Click();
			AssertText("Объект успешно удален");
			//проверяем что в базе данных тоже удалился
			var successfulEditTVChannelGroup = DbSession.Query<TvChannelGroup>().FirstOrDefault(p => p.Name == "Детская");
			Assert.That(successfulEditTVChannelGroup, Is.EqualTo(null), "Группа каналов должена удалиться и в базе данных");

		}

		[Test, Description("Проверка неудачного удаления группы TV-каналов (при условии что она прикреплена к каналам)")]
		public void UnsuccessfulEditTVChannelGroup()
		{
			Open("Plans/TVChannelGroupList");
			var targetTvChannelGroup = browser.FindElementByXPath("//td[contains(.,'Все')]");
			var row = targetTvChannelGroup.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-red"));
			button.Click();
			AssertText("Объект не удалось удалить! Возможно уже был связан с другими объектами");
			var UnsuccessfulEditTVChannelGroup = DbSession.Query<TvChannelGroup>().FirstOrDefault(p => p.Name == "Все");
			Assert.That(UnsuccessfulEditTVChannelGroup.Name, Does.Contain("Все"), "Группа TV-каналов должена все еще быть сохраненная в базе данных");
		}
	}
}