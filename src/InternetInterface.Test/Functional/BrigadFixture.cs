using InternetInterface.Models;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class BrigadFixture : SeleniumFixture
	{
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
