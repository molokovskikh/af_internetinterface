using InternetInterface.Models;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class BrigadFixture : SeleniumFixture
	{
		[Test, Ignore("Был в игноре в watin")]
		public void BrigadTest()
		{
			Open("Brigads/ShowBrigad.rails");
			AssertText("ID");
			AssertText("Имя");
			Css("#RegisterBrigad").Click();
			WaitForCss("#Name");
			Css("#Name").SendKeys("TestBrigad");
			Css("#RegisterBrigadButton").Click();
			foreach (var brigad in Brigad.FindAllByProperty("Name", "TestBrigad")) {
				session.Delete(brigad);
			}
			session.Flush();
		}

		[Test]
		public void ReportOnWork()
		{
			Open("Brigads/ReportOnWork");
			AssertText("Параметры фильтрации");
			AssertText("Статистика подключений");
			AssertText("Всего");
			AssertText("Не состоявшихся");
			Click("Показать");
			AssertText("Параметры фильтрации");
		}
	}
}
