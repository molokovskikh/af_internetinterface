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
using Common.Web.Ui.Helpers;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Initializers;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;
using log4net.Config;
using Test.Support;

namespace InternetInterface.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		private Server _webServer;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			SeleniumFixture.GlobalSetup();
			ConfigTest();
			SeedDb();

			InitializeContent.GetPartner = () => Partner.FindFirst();
			_webServer = SeleniumFixture.StartServer();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			SeleniumFixture.GlobalTearDown();
			_webServer.ShutDown();
		}

		public static void SeedDb()
		{
			using (new SessionScope()) {
				ArHelper.WithSession(session => {
					var settings = session.Query<InternetSettings>().FirstOrDefault();
					if (settings == null)
						session.Save(new InternetSettings());

					if (!session.Query<SaleSettings>().Any()) {
						session.Save(SaleSettings.Defaults());
					}

					var self = session.Query<Partner>().FirstOrDefault(p => p.Login == Environment.UserName);
					if (self == null) {
						self = new Partner(Environment.UserName, session.Load<UserRole>(3u));
						session.Save(self);
					}
					if (!session.Query<Tariff>().Any())
						session.Save(new Tariff("Тариф для тестирования", 500));

					if (!session.Query<Brigad>().Any()) {
						session.Save(new Brigad("Бригада для тестирования"));
						var partner = new Partner {
							Name = "Сервисный инженер для тестирования",
							Login = "test_serviceman",
							Role = session.Query<UserRole>().First(c => c.ReductionName == "service")
						};
						session.Save(partner);
					}

					if (!session.Query<Zone>().Any()) {
						var zone = new Zone("Тестовая зона");
						session.Save(zone);
					}

					if (!session.Query<RegionHouse>().Any(r => r.Name == "Воронеж"))
						session.Save(new RegionHouse("Воронеж"));
					session.Flush();
					var zones = session.Query<Zone>();
					foreach (var zone in zones) {
						if (zone.Name.ToLower().Contains("воронеж")) {
							zone.Region = session.Query<RegionHouse>().First(i => i.Name.ToLower().Contains("воронеж"));
							session.Save(zone);
						}
					}
					session.Flush();
					if (!session.Query<House>().Any()) {
						var region = new RegionHouse("Тестовый регион");
						session.Save(region);
						session.Save(new House("Тестовая улица", 1, region));
					}
				});
			}
		}

		public static void ConfigTest()
		{
			XmlConfigurator.Configure();
			if (!ActiveRecordStarter.IsInitialized) {
				ActiveRecordInitialize.Init(ConnectionHelper.GetConnectionName(),
					new[] {
						Assembly.Load("InternetInterface"),
						Assembly.Load("InternetInterface.Test")
					},
					new[] { typeof(ValidEventListner) });
				IntegrationFixture2.Factory = Activ
			}
		}
	}
}