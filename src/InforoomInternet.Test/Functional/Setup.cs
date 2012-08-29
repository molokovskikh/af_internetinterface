using System;
using System.IO;
using System.Text;
using CassiniDev;
using NUnit.Framework;
using System.Configuration;
using Test.Support.Web;
using WatiN.Core;
using log4net;

namespace InforoomInternet.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		private Server _webServer;

		[SetUp]
		public void SetupFixture()
		{
			_webServer = WatinSetup.StartServer();
			InternetInterface.Test.Functional.Setup.PrepareTestData();
		}

		[TearDown]
		public void TeardownFixture()
		{
			_webServer.ShutDown();
		}
	}
}