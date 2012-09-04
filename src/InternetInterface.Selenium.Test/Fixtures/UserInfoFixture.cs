using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InternetInterface.Selenium.Test
{
	[TestFixture]
	public class UserInfoFixture : InternetSeleniumFixture
	{
		[Test, Ignore("Временно, для релиза немедленного требования")]
		public void Remake_virginity_client_test()
		{
			using (var browser = OpenSelenium(ClientUrl)) {
				//Assert.IsNull(browser.FindElement(By.Id("naznach_but")));
				browser.FindElement(By.Id("clearGraphButton")).Click();
				Assert.IsNotNull(browser.FindElement(By.Id("naznach_but")));
			}
		}
	}
}
