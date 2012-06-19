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
using Test.Support.Web;
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
			PrepareTestData();

			_webServer = WatinSetup.StartServer();
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
	}
}