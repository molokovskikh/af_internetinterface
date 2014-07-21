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

		[SetUp]
		public void SetupFixture()
		{
			SeleniumFixture.GlobalSetup();
			_webServer = SeleniumFixture.StartServer();
			InternetInterface.Test.Functional.Setup.SeedDb();
		}

		[TearDown]
		public void TeardownFixture()
		{
			SeleniumFixture.GlobalTearDown();
			_webServer.ShutDown();
		}
	}
}