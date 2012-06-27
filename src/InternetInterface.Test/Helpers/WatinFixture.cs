using System;
using System.Configuration;
using Castle.ActiveRecord;
using Common.Tools.Calendar;
using WatiN.Core.UtilityClasses;
using NUnit.Framework;
using WatiN.Core;
using WatiN.CssSelectorExtensions;

namespace InternetInterface.Test.Helpers
{
	[Obsolete("Устарел используй Test.Support.Web.WatinFixture2")]
	public class WatinFixture2 : WatinFixture
	{
		public WatinFixture2()
		{
			UseTestScope = true;
			SaveBrowser = true;
		}

		protected void Refresh()
		{
			if (scope != null)
				scope.Flush();
			browser.Refresh();
		}
	}

	[TestFixture, Obsolete("Устарел используй Test.Support.Web.WatinFixture2")]
	public class WatinFixture
	{

		protected bool UseTestScope;
		protected bool SaveBrowser;

		protected ISessionScope scope;
		protected Browser browser;

		[SetUp]
		public void Setup()
		{
			if (UseTestScope)
				scope = new SessionScope(FlushAction.Never);
		}

		[TearDown]
		public void Teardown()
		{
			if (scope != null)
			{
				scope.Dispose();
				scope = null;
			}

			if (browser != null)
			{
				browser.Dispose();
				browser = null;
			}
		}

		public static string BuildTestUrl(string urlPart)
		{
			if (!urlPart.StartsWith("/"))
				urlPart = "/" + urlPart;
			return String.Format("http://localhost:{0}{1}",
				ConfigurationManager.AppSettings["webPort"],
				urlPart);
		}

		protected static void CheckForError(IE browser)
		{
			if (browser.ContainsText("Error"))
			{
			}
		}

		protected IE Open(string uri = "/")
		{
			if (scope != null)
				scope.Flush();

			var browser = new IE(BuildTestUrl(uri));

			if (SaveBrowser)
			{
				if (this.browser != null)
					this.browser.Dispose();

				this.browser = browser;

				new TryFuncUntilTimeOut(2.Second()) {
						SleepTime = TimeSpan.FromMilliseconds(50.0),
						ExceptionMessage = () => string.Format("waiting {0} seconds for document text not null.", 2)
					}.Try(() => browser.Text != null);
			}
			return browser;
		}

		protected dynamic Css(string selector)
		{
			return browser.CssSelect(selector);
		}

		protected IE Open(string uri, params object[] args)
		{
			return Open(String.Format(uri, args));
		}
	}
}
