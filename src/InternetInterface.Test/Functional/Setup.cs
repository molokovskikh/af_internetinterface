using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Hosting;
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
			SetupEnvironment(_webServer);
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

				if (!ActiveRecordLinqBase<Brigad>.Queryable.Any()) {
					new Brigad("Бригада для тестирования").Save();
					new Partner {
						Name = "Сервисный инженер для тестирования",
						Login = "test_serviceman",
						Categorie = UserCategorie.Queryable.First(c => c.ReductionName == "service")
					}.Save();
				}

				if (!ActiveRecordLinqBase<Zone>.Queryable.Any()) {
					var zone = new Zone {
						Name = "Тестовая зона"
					};
					ActiveRecordMediator.Save(zone);
				}
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

		public static void SetupEnvironment(Server server)
		{
			var method = server.GetType().GetMethod("GetHost", BindingFlags.Instance | BindingFlags.NonPublic);
			method.Invoke(server, null);

			var manager = ApplicationManager.GetApplicationManager();
			var apps = manager.GetRunningApplications();
			var domain = manager.GetAppDomain(apps.Single().ID);
			domain.SetData("environment", "test");
		}
	}
}