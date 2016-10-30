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
using InternetInterface.Controllers;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomInternet.Test.Integration
{
	[TestFixture]
	public class MainControllerFixture : ControllerFixture
	{
		private MainController controller;

		private Client client;
		private NetworkSwitch networkSwitch;
		private ClientEndpoint endpoint;
		private Lease lease;
		private List<object> deleteOnTeardown;

		[SetUp]
		public void Setup()
		{
			controller = new MainController();
			Prepare(controller);

			session.Delete("from Lease");

			var settings = new Settings(session);
			client = ClientHelper.Client(session);
			networkSwitch = new NetworkSwitch("Тестовый коммутатор", session.Query<Zone>().First());
			endpoint = new ClientEndpoint(client, 1, networkSwitch);
			client.AddEndpoint(endpoint, settings);
			var pool = new IpPool {
				IsGray = true,
				Begin = IPAddress.Parse("192.168.1.1").ToBigEndian(),
				End = IPAddress.Parse("192.168.1.100").ToBigEndian(),
			};
			lease = new Lease {
				Endpoint = endpoint,
				Switch = networkSwitch,
				Port = 1,
				Ip = IPAddress.Parse("192.168.1.2"),
				Pool = pool
			};

			deleteOnTeardown = new List<object> {
				client, networkSwitch, endpoint, lease, pool
			};
			session.SaveMany(deleteOnTeardown.ToArray());
		}

		[TearDown]
		public void Teardown()
		{
			session.DeleteMany(deleteOnTeardown.ToArray());
		}

		[Test]
		public void Watning_actual_package_id()
		{
			lease.Endpoint.SetStablePackgeId(15);

			Request.UserHostAddress = "192.168.1.2";
			Request.HttpMethod = "POST";
			controller.Warning();

			Assert.AreEqual(lease.Endpoint.ActualPackageId, 15);
		}

		[Test]
		public void Show_warning_for_static()
		{
			session.Save(new StaticIp(endpoint, "91.209.124.50"));
			Request.UserHostAddress = "91.209.124.50";
			controller.Warning();
			Assert.IsFalse(Response.WasRedirected);
			Assert.AreEqual(controller.PropertyBag["client"], client);
		}

		[Test]
		public void Show_warning_page_without_referer()
		{
			Request.UserHostAddress = "192.168.1.2";
			Request.QueryString["n"] = "91.235.90.57@Anonymous";
			controller.Warning();
			Assert.AreEqual("", ControllerContext.PropertyBag["referer"]);
		}
	}
}