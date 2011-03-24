using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CassiniDev;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Config;
using InternetInterface.Test.Helpers;
using NHibernate.Cfg;
using NUnit.Framework;
using System.Configuration;
using Settings = WatiN.Core.Settings;

namespace InternetInterface.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		private Server _webServer;

		[SetUp]
		public void SetupFixture()
		{

			//WatinFixture.ConfigTest();

			var port = int.Parse(ConfigurationManager.AppSettings["webPort"]);
			var webDir = ConfigurationManager.AppSettings["webDirectory"];

			_webServer = new Server(port, "/", Path.GetFullPath(webDir));
			_webServer.Start();
			Settings.Instance.AutoMoveMousePointerToTopLeft = false;
			Settings.Instance.MakeNewIeInstanceVisible = false;
		}

		[TearDown]
		public void TeardownFixture()
		{
			_webServer.ShutDown();
		}
	}
}