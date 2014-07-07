using System;
using System.Linq;
using Headless;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class ConnectionRequestFixture : HeadlessFixture
	{
		[Test]
		public void Create_request()
		{
			var page = Open();
			page = Click(page, "Регистрация");
			Assert.That(page.Html, Is.StringContaining("Регистрация новой заявки на подключение"));
			Input(page, "request_ApplicantName", "Ребкин Вадим Леонидович");
			Input(page, "request_ApplicantPhoneNumber", "8-473-606-20-00");
			Input(page, "request_Street", "Суворова");
			Input(page, "request_House", "1");
			Input(page, "request_Apartment", "1");
			page = ClickButton(page, "Сохранить");
			Assert.That(page.Html, Is.StringContaining("Сохранено"));
		}

		private DynamicHtmlPage ClickButton(DynamicHtmlPage page, string name)
		{
			var button = page.Find<HtmlButton>().All().First(e => e.Value == name);
			return button.Click<DynamicHtmlPage>();
		}

		private static string Input(DynamicHtmlPage page, string id, string value)
		{
			return page.Find<HtmlInput>().ById(id).Value = value;
		}
	}

	public class HeadlessFixture
	{
		protected Browser browser;

		[SetUp]
		public void Setup()
		{
			browser = new Browser();
		}

		public static DynamicHtmlPage Click(DynamicHtmlPage page, string name)
		{
			var link = page.Find<HtmlLink>().All().First(l => l.Text == name);
			page = link.Click();
			return page;
		}

		protected DynamicHtmlPage Open(string url = "Map/SiteMap")
		{
			return browser.GoTo<DynamicHtmlPage>(new Uri(SeleniumFixture.GetUri(url)));
		}
	}
}