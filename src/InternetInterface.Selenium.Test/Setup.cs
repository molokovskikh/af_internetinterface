using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OpenQA.Selenium.Chrome;

namespace InternetInterface.Selenium.Test
{
	[SetUpFixture]
	public class Setup : InternetInterface.Test.Functional.Setup
	{
		[SetUp]
		public void SeleniumSetup()
		{
			SeleniumFixture.GlobalDriver = new ChromeDriver("../../../../lib/");
		}

		[TearDown]
		public void SeleniumTearDown()
		{
			if (SeleniumFixture.GlobalDriver != null) {
				SeleniumFixture.GlobalDriver.Quit();
				SeleniumFixture.GlobalDriver.Dispose();
				SeleniumFixture.GlobalDriver = null;
			}
		}
	}
}
