using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using NUnit.Framework;
using Test.Support.Web;
using WatiN.Core;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class PartnerFixture : WatinFixture2
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
			AssertText("Введите такст закладки");
			browser.TextField("bookmark_Text").AppendText("текст тестовой закладки");
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
			session.SaveOrUpdate(bookmark);
			Flush();
			Click("Закладки");
			browser.TextField(Find.ByName("Period.Begin")).AppendText(DateTime.Now.AddDays(4).ToShortDateString());
			browser.TextField(Find.ByName("Period.End")).AppendText(DateTime.Now.AddDays(6).ToShortDateString());
			Click("Показать");
			Click("Удалить");
			AssertText("Удалено");
		}
	}
}
