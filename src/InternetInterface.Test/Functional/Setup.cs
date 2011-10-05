using System;
using System.Configuration;
using System.IO;
using System.Text;
using CassiniDev;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;
using log4net;

namespace InternetInterface.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{

		private static readonly ILog _log = LogManager.GetLogger(typeof(Setup));

		private Server _webServer;

		[SetUp]
		public void SetupFixture()
		{	
			WatinFixture.ConfigTest();

			var port = int.Parse(ConfigurationManager.AppSettings["webPort"]);

			var webDir = string.Empty;
			if (Environment.MachineName.ToLower() == "devsrv")
				webDir = ConfigurationManager.AppSettings["webDirectoryDev"];
			else
				webDir = ConfigurationManager.AppSettings["webDirectory"];

			var err = new StringBuilder();
			err.AppendLine("SERVERNAME " + webDir);
			err.AppendLine("MASHINE " + Environment.MachineName.ToLower());
			err.AppendLine("FULLPATH " + Path.GetFullPath(webDir));
			_log.Error(err.ToString());

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