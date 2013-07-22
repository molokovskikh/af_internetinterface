using System;
using System.Collections.Generic;
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
		private List<object> deleteOnTeardown;

		[SetUp]
		public void Setup()
		{
			controller = new MainController();
			PrepareController(controller);

			scope = new SessionScope();
			sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			session = sessionHolder.CreateSession(typeof(ActiveRecordBase));

			session.Delete("from Lease");
			client = ClientHelper.Client();
			networkSwitch = new NetworkSwitch("Тестовый коммутатор", session.Query<Zone>().First());
			endpoint = new ClientEndpoint(client, 1, networkSwitch);
			var pool = session.Query<IpPool>().First();
			lease = new Lease {
				Endpoint = endpoint,
				Switch = networkSwitch,
				Port = 1,
				Ip = IPAddress.Loopback,
				Pool = pool
			};

			deleteOnTeardown = new List<object> {
				client, networkSwitch, endpoint, lease
			};
			session.SaveMany(deleteOnTeardown.ToArray());

			controller.DbSession = session;
		}

		[TearDown]
		public void TearDown()
		{
			session.DeleteMany(deleteOnTeardown.ToArray());
			sessionHolder.ReleaseSession(session);
			scope.Dispose();
		}

		[Test]
		public void Warning_package_id()
		{
			controller.DbSession = session;
			controller.WarningPackageId();
		}

		[Test]
		public void Watning_actual_package_id()
		{
			lease.Ip = IPAddress.Loopback;
			lease.Endpoint.PackageId = 15;
			session.SaveOrUpdate(lease);

			Request.HttpMethod = "POST";
			controller.Warning();

			Assert.IsNotNull(lease.Endpoint.ActualPackageId);
			Assert.AreEqual(lease.Endpoint.ActualPackageId.Value, 15);
		}

		//пока человек медитирует на страницу
		//его компьютер получает новую аренду
		//но запрос отправляет со старого адреса
		//тк время жизни для серых аренд мало все это проиходит пока человек думает
		[Test(Description = "Симулируется ситуацию когда ivrn и dhcp конкурируют, dhcp удаляет аренду по клиент медитирует на страницу")]
		public void Complete_after_long_wait()
		{
			deleteOnTeardown.Remove(lease);
			session.Delete(lease);
			Request.HttpMethod = "POST";
			Request.Form["origin"] = "localhost";
			controller.Complete();

			Assert.IsTrue(Response.WasRedirected);
			Assert.AreEqual("http://localhost", Response.RedirectedTo);
		}

		[Test]
		public void Show_warning_page_without_referer()
		{
			Request.QueryString["n"] = "91.235.90.57@Anonymous";
			controller.Warning();
			Assert.AreEqual("", ControllerContext.PropertyBag["referer"]);
		}
	}
}