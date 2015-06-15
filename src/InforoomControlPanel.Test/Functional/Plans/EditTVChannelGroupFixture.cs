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
	class EditTVChannelGroupFixture : PlanFixture
	{

		[Test, Description("Изменение группы TV-каналов")]
		public void EditTVChannelGroup()
		{
			Open("Plans/TVChannelGroupList");
			var tvChannelGroup = DbSession.Query<TvChannelGroup>().First(p => p.Name == "Спорт");
			var targetTvChannelGroup = browser.FindElementByXPath("//td[contains(.,'" + tvChannelGroup.Name + "')]");
			var row = targetTvChannelGroup.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			var tvChannelGroupName = browser.FindElementByCssSelector("input[id=TvChannelGroup_Name]");
			tvChannelGroupName.Clear();
			tvChannelGroupName.SendKeys("Спорт Изменен");
			browser.FindElementByCssSelector(".btn-green.save").Click();
			AssertText("Объект успешно изменен!");
			DbSession.Refresh(tvChannelGroup);
			Assert.That(tvChannelGroup.Name, Is.StringContaining("Спорт Изменен"), "Изменения группы каналов должны сохраниться и в базе данных");
		}


		[Test, Description("Добавление TV-канала в группу")]
		public void AddTVChannelInGroup()
		{
			Open("Plans/TVChannelGroupList");
			var tvChannelGroup = DbSession.Query<TvChannelGroup>().First(p => p.Name == "Развлечения");
			var targetTvChannelGroup = browser.FindElementByXPath("//td[contains(.,'" + tvChannelGroup.Name + "')]");
			var row = targetTvChannelGroup.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			Css("#TvChannelDropDown").SelectByText("Культура");
			browser.FindElementByCssSelector(".btn-green.add").Click();
			AssertText("Объект успешно прикреплен!");
			var TVChannelInGroup = browser.FindElementByCssSelector(".row.tvChannels").Text;
			Assert.That(TVChannelInGroup, Is.StringContaining("Культура"), "Добавленный канал должен отобразиться на странице в списке включенных каналов");
			browser.FindElementByCssSelector(".btn-green.save").Click();
			DbSession.Refresh(tvChannelGroup);
			var tVChannel = tvChannelGroup.TvChannels.ToList();
			var createTVChannel = tVChannel.FirstOrDefault(r => r.Name == "Культура");
			Assert.That(createTVChannel.Name, Is.StringContaining("Культура"), "В базе данных к группе должен быть прикреплен канал, который в нее включили");		
		}

		[Test, Description("Удаление TV-канала из группы")]
		public void DeleteTVChannelInGroup()
		{
			Open("Plans/TVChannelGroupList");
			var tvChannelGroup = DbSession.Query<TvChannelGroup>().First(p => p.Name == "Основная");
			var targetTvChannelGroup = browser.FindElementByXPath("//td[contains(.,'" + tvChannelGroup.Name + "')]");
			var row = targetTvChannelGroup.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			var TVChannel = DbSession.Query<TvChannel>().First(p => p.Name == "СТС");
			var targetTvChannel = browser.FindElementByXPath("//div[contains(.,'" + TVChannel.Name + "')]");
			var rowTvChannel = targetTvChannel.FindElement(By.XPath(".."));
			var buttonTvChannel = rowTvChannel.FindElement(By.CssSelector(".entypo-cancel-circled"));
			buttonTvChannel.Click();
			browser.FindElementByCssSelector(".btn-green.save").Click();
			DbSession.Refresh(tvChannelGroup);
			var tVChannel = tvChannelGroup.TvChannels.ToList();
			var createTVChannel = tVChannel.FirstOrDefault(r => r.Name == "СТС");
			Assert.That(createTVChannel, Is.Null, "В базе данных у группы должен быть удален канал");
		}
	}
}