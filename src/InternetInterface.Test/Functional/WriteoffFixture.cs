using System;
using System.Linq;
using Headless;
using NUnit.Framework;
using Rhino.Mocks;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class WriteoffFixture
	{
		[Test]
		public void View_write_off()
		{
			var browser = new Browser();
			var page = browser.GoTo<DynamicHtmlPage>(new Uri(SeleniumFixture.GetUri("Map/SiteMap")));
			var link = page.Find<HtmlLink>().All().First(l => l.Text == "Списания");
			page = link.Click();
			Assert.That(page.Html, Is.StringContaining("Имя клиента"));
		}
	}
}