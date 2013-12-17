using System;
using InternetInterface.Models;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class BookmarksFixture : SeleniumFixture
	{
		[SetUp]
		public void SetUp()
		{
			Open("Map/SiteMap");
		}

		[Test]
		public void Create_bookmark()
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
		public void Delete_bookmark()
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
