using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Web.Http;
using System.Web.Http.SelfHost;
using CassiniDev;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace Inforoom2.Test.Functional
{
	[SetUpFixture]
	public class Setup
	{
		
		public static Uri Url;
		public static ISession session = NHibernateActionFilter.sessionFactory.OpenSession();
	
	
		private Server _webServer;

		[SetUp]
		public void SetupFixture()
		{
			SeleniumFixture.GlobalSetup();
			_webServer = SeleniumFixture.StartServer();
			SeedDb();
		}

		[TearDown]
		public void TeardownFixture()
		{
			SeleniumFixture.GlobalTearDown();
			_webServer.ShutDown();
		}
		
		public static void SeedDb()
		{
			var pass = PasswordHasher.Hash("password");

			if (session.Query<Client>().Count() == 0) {
				var client = new Client {
					City = "Борисоглебск",
					Username = "client1",
					Password = pass.Hash,
					Salt = pass.Salt
				};
				session.Save(client);
			}

			if (session.Query<Employee>().Count() == 0) {
				var emp = new Employee() {
					Username = "admin1",
					Password = pass.Hash,
					Salt = pass.Salt,
				};

				session.Save(emp);
			}
		}
	}
}