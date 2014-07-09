using System;
using System.Linq;
using Headless;
using NUnit.Framework;
using Test.Support;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class ConnectionRequestFixture : HeadlessFixture
	{
		[Test]
		public void Create_request()
		{
			Open();
			Click(page, "Регистрация");
			AssertText("Регистрация новой заявки на подключение");
			Input("request_ApplicantName", "Ребкин Вадим Леонидович");
			Input("request_ApplicantPhoneNumber", "8-473-606-20-00");
			Input("request_Street", "Суворова");
			Input("request_House", "1");
			Input("request_Apartment", "1");
			ClickButton("Сохранить");
			AssertText("Сохранено");
		}
	}

	public class HeadlessFixture : IntegrationFixture
	{
		protected Browser browser;
		protected DynamicHtmlPage page;

		[SetUp]
		public void Setup()
		{
			browser = new Browser();
			page = null;
		}

		public static DynamicHtmlPage Click(DynamicHtmlPage page, string name)
		{
			var link = page.Find<HtmlLink>().All().First(l => l.Text == name);
			page = link.Click();
			return page;
		}

		public void Click(string name)
		{
			var link = page.Find<HtmlLink>().All().First(l => l.Text == name);
			page = link.Click<DynamicHtmlPage>();
		}

		protected DynamicHtmlPage Open(string url = "Map/SiteMap")
		{
			session.Flush();
			session.Transaction.Commit();
			page = browser.GoTo<DynamicHtmlPage>(new Uri(SeleniumFixture.GetUri(url)));
			return page;
		}

		protected void AssertText(string text)
		{
			Assert.That(page.Html, Is.StringContaining(text));
		}

		protected string Input(string id, string value)
		{
			return page.Find<HtmlInput>().ById(id).Value = value;
		}

		protected void ClickButton(string name)
		{
			var button = page.Find<HtmlButton>().All().First(e => e.Value == name);
			page = button.Click<DynamicHtmlPage>();
		}
	}
}