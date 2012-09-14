using System;
using System.Linq;
using System.Net;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.MonoRail.Framework.Test;
using Castle.MonoRail.TestSupport;
using Common.Web.Ui.NHibernateExtentions;
using InforoomInternet.Controllers;
using InternetInterface.Helpers;
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

		private Client client;
		private NetworkSwitch networkSwitch;
		private ClientEndpoint endpoint;
		private Lease lease;

		[SetUp]
		public void Setup()
		{
			controller = new MainController();
			PrepareController(controller);

			scope = new SessionScope();
			sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			session = sessionHolder.CreateSession(typeof(ActiveRecordBase));

			client = ClientHelper.Client();
			networkSwitch = new NetworkSwitch("Тестовый коммутатор", session.Query<Zone>().First());
			endpoint = new ClientEndpoint(client, 1, networkSwitch);
			lease = new Lease { Endpoint = endpoint, Switch = networkSwitch, Port = 1, Ip = (uint)new Random().Next() };

			session.SaveMany(client, networkSwitch, endpoint, lease);

			controller.DbSession = session;
		}

		[TearDown]
		public void TearDown()
		{
			session.DeleteMany(client, networkSwitch, endpoint, lease);
			sessionHolder.ReleaseSession(session);
			scope.Dispose();
		}

		[Test]
		public void Warning_package_id()
		{
			client = ClientHelper.Client();
			networkSwitch = new NetworkSwitch("Тестовый коммутатор", session.Query<Zone>().First());
			endpoint = new ClientEndpoint(client, 1, networkSwitch);
			lease = new Lease { Endpoint = endpoint, Switch = networkSwitch, Port = 1, Ip = (uint)new Random().Next() };

			session.Save(networkSwitch);
			session.Save(lease);
			controller.DbSession = session;
			controller.WarningPackageId();
		}

		[Test]
		public void Watning_actual_package_id()
		{
			var addressValue = BigEndianConverter.ToInt32(IPAddress.Parse("127.0.0.1").GetAddressBytes());
			lease.Ip = addressValue;
			lease.Endpoint.PackageId = 15;
			session.SaveOrUpdate(lease);

			((IMockRequest)controller.Request).HttpMethod = "POST";
			controller.Warning();

			Assert.IsNotNull(lease.Endpoint.ActualPackageId);
			Assert.AreEqual(lease.Endpoint.ActualPackageId.Value, 15);
		}
	}
}