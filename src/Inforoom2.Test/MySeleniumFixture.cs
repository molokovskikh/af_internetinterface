using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Web.Hosting;
using CassiniDev;
using Common.Tools.Calendar;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Internal;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using Test.Support;

namespace Inforoom2.Test
{
	public class MySeleniumFixture : IntegrationFixture
	{
		public class ChromeOptionsWithPrefs : ChromeOptions
		{
			public Dictionary<string, object> prefs { get; set; }
		}

		public static void GlobalSetup()
		{
			WebPort = Int32.Parse(ConfigurationManager.AppSettings["webPort"]);
			WebRoot = ConfigurationManager.AppSettings["webRoot"] ?? "/";
			if (GlobalDriver != null)
				return;

			var version = Directory.GetDirectories("../../../../packages/", "*ChromeDriver*").FirstOrDefault();
			var chromeOptions = new ChromeOptionsWithPrefs();
			chromeOptions.prefs = new Dictionary<string, object>
			{
				{"download.prompt_for_download", "false"},
				{"download.default_directory", Environment.CurrentDirectory}
			};
			chromeOptions.BinaryLocation = "../../../../lib/GoogleChromePortable/GoogleChromePortable.exe";
			GlobalDriver = new ChromeDriver(String.Format("{0}/content/", version), chromeOptions);
			GlobalDriver.Manage().Window.Size = new Size(1200, 1000);
		}

		public static void GlobalTearDown()
		{
			if (GlobalDriver != null) {
				GlobalDriver.Close();
				GlobalDriver.Quit();
				GlobalDriver.Dispose();
				GlobalDriver = null;
			}
		}

		private RemoteWebDriver _browser;
		private const int waitTime = 5;
		public static RemoteWebDriver GlobalDriver;
		public string defaultUrl = "/";
		private static string WebRoot;
		private static int WebPort;
		private static string WebDir;

		public RemoteWebDriver browser
		{
			get
			{
				if (_browser == null)
					_browser = Open();

				return _browser;
			}
		}

		public static string GetUri(string uri)
		{
			Uri uriObj;
			Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out uriObj);
			if (uriObj != null && !uriObj.IsAbsoluteUri)
				uri = BuildTestUrl(uri);
			return uri;
		}

		public static string BuildTestUrl(string urlPart)
		{
			if (!urlPart.StartsWith("/"))
				urlPart = "/" + urlPart;
			if (WebRoot.EndsWith("/") && urlPart.Length > 0)
				urlPart = urlPart.Remove(0, 1);
			return String.Format("http://localhost:{0}{1}{2}",
				WebPort,
				WebRoot,
				urlPart);
		}

		protected virtual string GetShortUrl(object item, string action = null)
		{
			var dynamicItem = ((dynamic) item);
			var id = dynamicItem.Id;
			var controller = AppHelper.GetControllerName(item);
			if (!String.IsNullOrEmpty(action))
				action = "/" + action;
			return String.Format("{0}/{1}{2}", controller, id, action);
		}

		protected RemoteWebDriver Open(object item, string action = null)
		{
			return Open(GetShortUrl(item, action));
		}

		protected RemoteWebDriver Open(string uri, params object[] args)
		{
			return Open(String.Format(uri, args));
		}

		public RemoteWebDriver Open(string url = null)
		{
			url = url ?? defaultUrl;
			if (scope != null) {
				scope.Flush();
				scope.Commit();
			}

			url = GetUri(url);

			if (_browser == null) {
				_browser = GlobalDriver;
			}

			browser.Navigate().GoToUrl(url);

			return _browser;
		}

		[TearDown]
		public void SeleniumTearDown()
		{
			var currentContext = TestContext.CurrentContext;
			if (currentContext.Result.Status == TestStatus.Failed) {
				if (browser != null) {
					Console.WriteLine(browser.Url);
					if (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEBUG_SELENIUM"))) {
						var data = browser.Url + Environment.NewLine + Html;
						File.WriteAllText(currentContext.Test.FullName + ".html", data);
						((ITakesScreenshot) browser).GetScreenshot().SaveAsFile(currentContext.Test.FullName + ".png", ImageFormat.Png);
					}
				}
			}
			_browser = null;
		}

		protected string Html
		{
			get { return browser.FindElementByTagName("body").GetAttribute("innerHTML"); }
		}

		protected void Refresh()
		{
			if (session != null)
				session.Flush();
			browser.Navigate().Refresh();
		}

		protected void RunJavaScript(string script)
		{
			(GlobalDriver as IJavaScriptExecutor).ExecuteScript(script);
		}

		protected void WaitForElementByLocator(By locator, int iterations = 100)
		{
			for (int i = 0; i < iterations; i++) {
				if (browser.FindElements(locator).Count > 0)
					break;
				Thread.Sleep(10);
			}
		}

		protected void WaitForCss(string css, int seconds = 5)
		{
			var wait = new WebDriverWait(browser, seconds.Second());
			wait.Until(d => ((RemoteWebDriver) d).FindElementsByCssSelector(css).Count > 0);
		}

		protected void WaitForVisibleCss(string css, int seconds = 10)
		{

			var wait = new WebDriverWait(browser, seconds.Second());
			wait.Until(d => ((RemoteWebDriver)d).FindElementsByCssSelector(css).Count > 0 && ((RemoteWebDriver) d).FindElementByCssSelector(css).Displayed);
		}

		protected void WaitAnimation(string css)
		{
			var wait = new WebDriverWait(browser, waitTime.Second());
			wait.Until(d =>
			{
				var javaScriptExecutor = (IJavaScriptExecutor) d;
				var isAnimated = bool.Parse(javaScriptExecutor
					.ExecuteScript(string.Format("return $('{0}').is(':animated')", css))
					.ToString().ToLower());
				return !isAnimated;
			});
		}

		protected void WaitClickable(string css)
		{
			var wait = new WebDriverWait(browser, waitTime.Second());
			wait.Until(d =>
			{
				var el = ((RemoteWebDriver) d).FindElementByCssSelector(css);
				return el.Displayed
				       && el.Enabled
					//проверяем что анимация завершилась
				       && el.Location.X > 0
				       && el.Location.Y > 0;
			});
		}

		protected void WaitForText(string text, int seconds = 7)
		{
			var wait = new WebDriverWait(browser, seconds.Second());
			wait.Until(d => ((RemoteWebDriver) d).FindElementByCssSelector("body").Text.Contains(text));
		}

		//иногда WaitForText приводит к ошибкам stale reference exception
		public void SafeWaitText(string text, int seconds = 7)
		{
			var begin = DateTime.Now;
			var timeout = seconds.Second();
			while (true) {
				try {
					var found = browser.FindElementByCssSelector("body").Text.Contains(text);
					if (found)
						break;
					if ((DateTime.Now - begin) > timeout)
						throw new Exception(String.Format("Не удалось дождаться текста '{0}'", text));
				}
				catch (StaleElementReferenceException) {
				}
				Thread.Sleep(50);
			}
		}

		protected dynamic FindByText(string text)
		{
			return browser.FindElementByXPath(String.Format("//*[contains(text(), {0})]", text));
		}

		protected void AssertText(string text)
		{
			var body = browser.FindElementByCssSelector("body").Text;
			Assert.That(body, Is.StringContaining(text));
		}

		protected void AssertNoText(string text)
		{
			var body = browser.FindElementByCssSelector("body").Text;
			Assert.That(body, Is.Not.StringContaining(text));
		}

		protected void AssertText(string text, string cssSelector, string errorMessage = "")
		{
			var body = browser.FindElementByCssSelector(cssSelector).Text;
			if (string.IsNullOrEmpty(errorMessage)) {
				Assert.That(body, Is.StringContaining(text));
			}
			else {
				Assert.That(body, Is.StringContaining(text), errorMessage);
			}
		}

		protected void AssertNoText(string text, string cssSelector, string errorMessage = "")
		{
			var body = browser.FindElementByCssSelector(cssSelector).Text;
			if (string.IsNullOrEmpty(errorMessage)) {
				Assert.That(body, Is.Not.StringContaining(text));
			}
			else {
				Assert.That(body, Is.Not.StringContaining(text), errorMessage);
			}
		}

		protected bool IsPresent(string selector)
		{
			return browser.FindElements(By.CssSelector(selector)).Count > 0;
		}

		protected dynamic Css(string selector)
		{
			return Css(browser, selector);
		}

		protected dynamic Css(dynamic parent, string selector)
		{
			var element = ((IFindsByCssSelector) parent).FindElementByCssSelector(selector);
			if (element.TagName == "select")
				return new SelectElement(element);
			return element;
		}

		protected void Click(string text)
		{
			var buttons = browser.FindElementsByCssSelector("a, input[type=button], input[type=submit], button");

			var button =
				buttons.FirstOrDefault(b => String.Equals(b.GetAttribute("value"), text, StringComparison.CurrentCultureIgnoreCase)) ??
				buttons.FirstOrDefault(b => String.Equals(b.Text, text, StringComparison.CurrentCultureIgnoreCase));

			if (button == null)
				throw new Exception(String.Format("Элемент с текстом '{0}' не найден!", text));

			button.Click();
		}

		protected void Click(IWebElement el, string text)
		{
			var buttons = el.FindElements(By.CssSelector("a, input[type=button], input[type=submit], button"));

			var button =
				buttons.FirstOrDefault(b => String.Equals(b.GetAttribute("value"), text, StringComparison.CurrentCultureIgnoreCase)) ??
				buttons.FirstOrDefault(b => String.Equals(b.Text, text, StringComparison.CurrentCultureIgnoreCase));

			if (button == null)
				throw new Exception(String.Format("Элемент с текстом '{0}' не найден!", text));

			button.Click();
		}


		protected void Click(By locator)
		{
			var element = browser.FindElement(locator);

			if (element == null)
				throw new Exception("Элемент не найден!");

			element.Click();
		}

		protected void ClickButton(string selector, string value)
		{
			var root = browser.FindElementByCssSelector(selector);
			var element = root.FindElement(By.CssSelector(String.Format("[value=\"{0}\"]", value)));
			browser.ExecuteScript(String.Format("window.scrollTo({0},{1})", element.Location.X, element.Location.Y));
			element.Click();
		}

		protected void ClickLink(string selector, string text)
		{
			var root = browser.FindElementByCssSelector(selector);
			root.FindElement(By.PartialLinkText(text)).Click();
		}

		protected void ClickButton(string value)
		{
			var element = browser.FindElement(By.CssSelector(String.Format("[value=\"{0}\"]", value)));
			browser.ExecuteScript(String.Format("window.scrollTo({0},{1})", element.Location.X, element.Location.Y));
			element.Click();
		}

		protected void ClickLink(string text)
		{
			browser.FindElementsByLinkText(text).First().Click();
		}

		protected object Eval(string js)
		{
			return ((IJavaScriptExecutor) browser).ExecuteScript(js);
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

		public static Server StartServer()
		{
			WebPort = Int32.Parse(ConfigurationManager.AppSettings["webPort"]);
			WebRoot = ConfigurationManager.AppSettings["webRoot"] ?? "/";
			WebDir = ConfigurationManager.AppSettings["webDirectory"];

			var webServer = new Server(WebPort, WebRoot, Path.GetFullPath(WebDir));
			webServer.Start();

			try {
				SetupEnvironment(webServer);
			}
			catch (Exception) {
				try {
					if (GlobalDriver != null) {
						GlobalDriver.Close();
						GlobalDriver.Quit();
						GlobalDriver.Dispose();
						GlobalDriver = null;
					}
				}
				catch (Exception exception) {
					Console.WriteLine(exception);
				}
				throw;
			}
			return webServer;
		}

		public static void SetupEnvironment(Server server)
		{
			var method = server.GetType().GetMethod("GetHost", BindingFlags.Instance | BindingFlags.NonPublic);
			method.Invoke(server, null);

			var manager = ApplicationManager.GetApplicationManager();
			var apps = manager.GetRunningApplications();
			var domain = manager.GetAppDomain(apps.Single().ID);
			domain.SetData("environment", "test");
		}

		public void WaitAjax(int seconds = 5)
		{
			new WebDriverWait(browser, seconds.Second())
				.Until(d => Convert.ToInt32(Eval("return $.active")) == 0);
		}
	}
}