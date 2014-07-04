using System;
using CassiniDev;
using NUnit.Framework;
using Test.Support.Selenium;
using Test.Support.Web;

namespace InforoomInternet.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		private Server _webServer;

		[SetUp]
		public void SetupFixture()
		{
			SeleniumFixture.GlobalSetup();
			_webServer = WatinSetup.StartServer();
			InternetInterface.Test.Functional.Setup.PrepareTestData();
		}

		[TearDown]
		public void TeardownFixture()
		{
			SeleniumFixture.GlobalTearDown();
			_webServer.ShutDown();
		}
	}
}