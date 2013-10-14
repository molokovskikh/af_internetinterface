using System;
using InternetInterface.Models;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class PartnerFixture : SeleniumFixture
	{
		[SetUp]
		public void SetUp()
		{
			Open("Map/SiteMap");
		}

		[Test]
		public void Base_bookmarks_test()
		{
			Click("Закладки");
			Click("Добавить закладку");
			Click("Сохранить");
			AssertText("Введите текст закладки");
			browser.FindElementById("bookmark_Text").SendKeys("текст тестовой закладки");
			Click("Сохранить");
			AssertText("Сохранено");
			Open("Map/SiteMap");
			AssertText("текст тестовой закладки");
		}

		[Test]
		public void Delete_bookmarks_test()
		{
			var bookmark = new Bookmark {
				Date = DateTime.Now.AddDays(5),
				Text = "Тест удаленой закладки"
			};
			session.Save(bookmark);

			Click("Закладки");
			browser.FindElementByName("Period.Begin").SendKeys(DateTime.Now.AddDays(4).ToShortDateString());
			browser.FindElementByName("Period.End").SendKeys(DateTime.Now.AddDays(6).ToShortDateString());
			Click("Показать");
			Click("Удалить");
			AssertText("Удалено");
		}
	}
}
