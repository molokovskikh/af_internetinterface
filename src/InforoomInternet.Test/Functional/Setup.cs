using System;
using System.IO;
using System.Text;
using CassiniDev;
using NUnit.Framework;
using System.Configuration;
using WatiN.Core;
using log4net;

namespace InforoomInternet.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		private Server _webServer;
		private static readonly ILog _log = LogManager.GetLogger(typeof(Setup));

		[SetUp]
		public void SetupFixture()
		{

			//WatinFixture.ConfigTest();

			var port = int.Parse(ConfigurationManager.AppSettings["webPort"]);

			var webDir = string.Empty;
			if (Environment.MachineName.ToLower() == "devsrv")
				webDir = ConfigurationManager.AppSettings["webDirectoryDev"];
			else
				webDir = ConfigurationManager.AppSettings["webDirectory"];

			var err = new StringBuilder();
			err.AppendLine("InternetInterface.Test");
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