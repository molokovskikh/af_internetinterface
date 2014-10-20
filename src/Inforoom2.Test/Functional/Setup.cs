using System;
using System.Collections;
using System.Collections.Generic;
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
			Permission permission = new Permission { Name = "TestPermission" };
			session.Save(permission);

			Role role = new Role { Name = "Admin" };
			session.Save(role);

			var pass = PasswordHasher.Hash("password");
			var client = new Client {
				City = "Воронеж",
				Username = "client",
				Password = pass.Hash,
				Salt = pass.Salt
			};
			session.Save(client);

			IList<Role> roles = new List<Role>();
			roles.Add(role);

			var emp = new Employee() {
				Username = "admin",
				Password = pass.Hash,
				Salt = pass.Salt,
				Roles = roles,
			};
				

			session.Save(emp);
			session.Flush();
		}
	}
}