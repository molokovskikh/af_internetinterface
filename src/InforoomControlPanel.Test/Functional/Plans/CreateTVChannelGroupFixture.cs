using System.Linq;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class CreateTVChannelGroupFixture : PlanFixture
	{
		[Test, Description("Добавление группы телевизионных каналов")]
		public void CreateTVChannelGroup()
		{
			Open("Plans/TvChannelGroupList");
			var tVChannelGroupCount = DbSession.Query<TvChannelGroup>().ToList().Count;
			browser.FindElementByCssSelector(".btn-green").Click();
			var tvChannelGroupName = browser.FindElementByCssSelector("input[id=TvChannelGroup_Name]");
			tvChannelGroupName.SendKeys("Тест");
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("Объект успешно добавлен!");
			var createTVChannelGroup =  DbSession.Query<TvChannelGroup>().First(p => p.Name == "Тест");
			Assert.That(createTVChannelGroup.Name, Does.Contain("Тест"), "Добавленный канал должен сохраниться и в базе данных");
			var tVChannelGroupCountAfterTest = DbSession.Query<TvChannelGroup>().ToList().Count;
			Assert.That(tVChannelGroupCountAfterTest, Is.EqualTo(tVChannelGroupCount + 1), "Количество групп телевизионных каналов должно увеличиться на один после добавления");
		}	
	}
}