using System;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.MonoRail.TestSupport;
using InforoomInternet.Controllers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomInternet.Test.Integration
{
	[TestFixture]
	public class MainControllerFixture : BaseControllerTest
	{
		private MainController controller;
		private SessionScope scope;
		private ISessionFactoryHolder sessionHolder;
		private ISession session;

		[SetUp]
		public void Setup()
		{
			controller = new MainController();
			PrepareController(controller);

			scope = new SessionScope();
			sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
		}

		[TearDown]
		public void TearDown()
		{
			sessionHolder.ReleaseSession(session);
			scope.Dispose();
		}

		[Test]
		public void Warning_package_id()
		{
			var client = ClientHelper.Client();
			var commutator = new NetworkSwitch("Тестовый коммутатор", session.Query<Zone>().First());
			var endpoint = new ClientEndpoint(client, 1, commutator);
			var lease = new Lease { Endpoint = endpoint, Switch = commutator, Port = 1, Ip = (uint)new Random().Next() };

			session.Save(commutator);
			session.Save(lease);
			controller.DbSession = session;
			controller.WarningPackageId();
		}
	}
}