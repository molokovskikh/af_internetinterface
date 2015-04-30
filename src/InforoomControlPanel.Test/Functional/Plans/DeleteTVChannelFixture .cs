using System.Linq;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class DeleteTVChannelFixture : PlanFixture
	{
		[Test, Description("Проверка успешного удаления канала (при условии,что он не прикреплен к группе)")]
		public void SuccessfulEditTVChannel()
		{
			Open("Plans/TVChannelList");
			var tvChannel = DbSession.Query<TvChannel>().First(p => p.Name == "ППТ");
			var targetTvChannel = browser.FindElementByXPath("//td[contains(.,'" + tvChannel.Name + "')]");
			var row = targetTvChannel.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-red"));
			button.Click();
			AssertText("Объект успешно удален");
			//проверяем что в базе данных тоже удалился
			var successfulEditTVChannel = DbSession.Query<TvChannel>().FirstOrDefault(p => p.Name == "ППТ");
			Assert.That(successfulEditTVChannel, Is.EqualTo(null),"Канал должен удалиться и в базе данных");

		}

		[Test, Description("Проверка неудачного удаления телевизионного канала (при условии что он прикреплен к группе)")]
		public void UnsuccessfulEditTVChannel()
		{
			Open("Plans/TVChannelList");
			var targetTvChannel = browser.FindElementByXPath("//td[contains(.,'СТС')]");
			var row = targetTvChannel.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-red"));
			button.Click();
			AssertText("Объект не удалось удалить! Возможно уже был связан с другими объектами");
			var UnsuccessfulEditTVChannel = DbSession.Query<TvChannel>().FirstOrDefault(p => p.Name == "ППТ");
			Assert.That(UnsuccessfulEditTVChannel.Name, Is.StringContaining("ППТ"), "Канал должен все еще быть сохраненным в базе данных");
		}


	}
}