using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using CassiniDev;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.MySql;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Models;
using NUnit.Framework;
using WatiN.Core;
using log4net.Config;

namespace InternetInterface.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		private Server _webServer;

		[SetUp]
		public void SetupFixture()
		{	
			ConfigTest();

			var port = Int32.Parse(ConfigurationManager.AppSettings["webPort"]);
			var webDir = ConfigurationManager.AppSettings["webDirectory"];

			_webServer = new Server(port, "/", Path.GetFullPath(webDir));
			_webServer.Start();
			Settings.Instance.AutoMoveMousePointerToTopLeft = false;
			Settings.Instance.MakeNewIeInstanceVisible = false;

			PrepareTestData();
		}

		[TearDown]
		public void TeardownFixture()
		{
			_webServer.ShutDown();
		}

		public static void PrepareTestData()
		{
			using(new SessionScope()) {

				var partner = ActiveRecordLinqBase<Partner>.Queryable.FirstOrDefault(p => p.Login == Environment.UserName);
				if (partner == null) {
					partner = new Partner(Environment.UserName);
					partner.Save();
				}

				if (!ActiveRecordLinqBase<Tariff>.Queryable.Any())
					new Tariff("Тариф для тестирования", 500).Save();
				if (!ActiveRecordLinqBase<Brigad>.Queryable.Any())
					new Brigad("Бригада для тестирования").Save();
			}
		}

		public static void ConfigTest()
		{
			XmlConfigurator.Configure();
			if (!ActiveRecordStarter.IsInitialized)
				ActiveRecordInitialize.Init(ConnectionHelper.GetConnectionName(),
					Assembly.Load("InternetInterface"),
					Assembly.Load("InternetInterface.Test"));
		}
	}
}