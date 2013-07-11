using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools.Calendar;
using Common.Web.Ui.ActiveRecordExtentions;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using Test.Support;
using Test.Support.Web;

namespace InternetInterface.Selenium.Test
{
	public class SeleniumFixture : IntegrationFixture
	{
		private RemoteWebDriver _browser;

		public static RemoteWebDriver GlobalDriver;
		public string defaultUrl = "/";

		public RemoteWebDriver browser
		{
			get
			{
				if (_browser == null)
					_browser = Open();

				return _browser;
			}
		}

		public RemoteWebDriver Open(string url = null)
		{
			url = url ?? defaultUrl;
			if (scope != null) {
				scope.Flush();
				scope.Commit();
			}

			url = WatinFixture2.GetUri(url);

			if (_browser == null) {
				_browser = GlobalDriver;
			}

			browser.Navigate().GoToUrl(url);

			return _browser;
		}

		[TearDown]
		public void TearDown()
		{
			_browser = null;
		}

		protected void WaitForCss(string css)
		{
			var wait = new WebDriverWait(browser, 3.Second());
			wait.Until(d => ((RemoteWebDriver)d).FindElementsByCssSelector(css).Count > 0);
		}

		protected void AssertText(string text)
		{
			var body = browser.FindElementByCssSelector("body").Text;
			Assert.That(body, Is.StringContaining(text));
		}

		protected dynamic Css(string selector)
		{
			var element = browser.FindElementByCssSelector(selector);
			if (element.TagName == "select")
				return new SelectElement(element);
			return element;
		}

		protected void ClickButton(string selector, string value)
		{
			var root = browser.FindElementByCssSelector(selector);
			root.FindElement(By.CssSelector(String.Format("[value=\"{0}\"]", value)))
				.Click();
		}

		protected void ClickLink(string selector, string text)
		{
			var root = browser.FindElementByCssSelector(selector);
			root.FindElement(By.PartialLinkText(text)).Click();
		}

		protected void ClickButton(string value)
		{
			browser.FindElement(By.CssSelector(String.Format("[value=\"{0}\"]", value)))
				.Click();
		}

		protected void ClickLink(string text)
		{
			browser.FindElementsByLinkText(text).First().Click();
		}

		//todo нужно ловить ошибки в js
		//private string GetError()
		//{
		//	return browser.Eval("window.errors");
		//}

		//private void AttachError()
		//{
		//	browser.Eval("window.errors = []; window.onerror = function(errorMsg, url, lineNumber) { window.errors.push({e: errorMsg, u: url, l: lineNumber}) };");
		//}
	}
}
