using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Web.Ui.ActiveRecordExtentions;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Test.Support;
using Test.Support.Web;

namespace InternetInterface.Selenium.Test
{
	public class SeleniumFixture : IntegrationFixture
	{
		public IWebDriver Browser;

		public IWebDriver OpenSelenium(string uri = "/")
		{
			if (scope != null) {
				scope.Flush();
				scope.Commit();
			}

			uri = WatinFixture2.GetUri(uri);

			if (Browser == null) {
				Browser = new ChromeDriver("../../../../lib/");
			}

			Browser.Navigate().GoToUrl(uri);

			return Browser;
		}

		[TearDown]
		public void TearDown()
		{
			if (Browser != null) {
				Browser.Quit();
				Browser.Dispose();
				Browser = null;
			}
		}
	}
}
