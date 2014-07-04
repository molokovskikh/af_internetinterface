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
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;
using Test.Support.Web;
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
			SeleniumFixture.GlobalSetup();
			ConfigTest();
			PrepareTestData();

			InitializeContent.GetPartner = () => Partner.FindFirst();
			_webServer = WatinSetup.StartServer();
		}

		[TearDown]
		public void TeardownFixture()
		{
			SeleniumFixture.GlobalTearDown();
			_webServer.ShutDown();
		}

		public static void PrepareTestData()
		{
			using (new SessionScope()) {
				ArHelper.WithSession(session => {
					var settings = session.Query<InternetSettings>().FirstOrDefault();
					if (settings == null)
						session.Save(new InternetSettings());

					var saleSettings = session.Query<SaleSettings>().FirstOrDefault();
					if (settings == null) {
						saleSettings = new SaleSettings {
							MinSale = 3,
							MaxSale = 15,
							PeriodCount = 3,
							SaleStep = 1
						};
						session.Save(saleSettings);
					}

					var self = session.Query<Partner>().FirstOrDefault(p => p.Login == Environment.UserName);
					if (self == null) {
						self = new Partner(Environment.UserName);
						session.Save(self);
					}
					if (!session.Query<Tariff>().Any())
						session.Save(new Tariff("Тариф для тестирования", 500));

					if (!session.Query<Brigad>().Any()) {
						session.Save(new Brigad("Бригада для тестирования"));
						var partner = new Partner {
							Name = "Сервисный инженер для тестирования",
							Login = "test_serviceman",
							Role = UserRole.Queryable.First(c => c.ReductionName == "service")
						};
						session.Save(partner);
					}

					if (!session.Query<Zone>().Any()) {
						var zone = new Zone("Тестовая зона");
						session.Save(zone);
					}

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
			if (!ActiveRecordStarter.IsInitialized)
				ActiveRecordInitialize.Init(ConnectionHelper.GetConnectionName(),
					Assembly.Load("InternetInterface"),
					Assembly.Load("InternetInterface.Test"));
		}
	}
}