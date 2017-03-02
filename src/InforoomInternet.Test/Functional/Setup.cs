using System;
using CassiniDev;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InforoomInternet.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		private Server _webServer;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			SeleniumFixture.GlobalSetup();
			_webServer = SeleniumFixture.StartServer();
			InternetInterface.Test.Functional.Setup.SeedDb();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			SeleniumFixture.GlobalTearDown();
			_webServer.ShutDown();
		}
	}
}