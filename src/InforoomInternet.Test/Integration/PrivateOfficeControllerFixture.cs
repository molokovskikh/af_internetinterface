using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Framework.Test;
using Castle.MonoRail.TestSupport;
using InforoomInternet.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Test.Helpers;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomInternet.Test.Integration
{
	[TestFixture]
	public class PrivateOfficeControllerFixture : BaseControllerTest
	{
		private PrivateOfficeController controller;

		private SessionScope scope;
		private ISessionFactoryHolder sessionHolder;
		private ISession session;

		private PhysicalClient physicalClient;
		private Client client;

		private string referer;

		protected override IMockResponse BuildResponse(UrlInfo info)
		{
			return new StubResponse(
				info,
				new DefaultUrlBuilder(),
				new StubServerUtility(),
				new RouteMatch(),
				referer);
		}

		[SetUp]
		public void Init()
		{
			referer = "http://www.ivrn.net/";
			InitializeContent.GetAdministrator = () => null;

			scope = new SessionScope();
			sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			session = sessionHolder.CreateSession(typeof(ActiveRecordBase));

			controller = new PrivateOfficeController();
			controller.DbSession = session;
			PrepareController(controller);

			physicalClient = ClientHelper.PhysicalClient();
			client = physicalClient.Client;

			session.Save(client);
			Context.Session["LoginClient"] = client.Id;
		}

		[TearDown]
		public void TearDown()
		{
			if (sessionHolder != null && session != null)
				sessionHolder.ReleaseSession(session);

			if (scope != null)
				scope.Dispose();
		}

		[Test]
		public void PrivateOffice()
		{
			using (new SessionScope()) {
				var filter = new AccessFilter();
				Request.UserHostAddress = "192.168.200.1";
				Assert.IsTrue(filter.Perform(ExecuteWhen.BeforeAction, controller.Context, controller, controller.ControllerContext));
			}
		}

		[Test]
		public void FirstVisitIfRequestOutAddress()
		{
			client.FirstLunch = false;
			session.Save(client);
			session.Flush();
			var addresses = GenerateIp();
			Request.UserHostAddress = addresses.First();
			var ipAdress = IPAddress.Parse(addresses.First());
			controller.Context.Session["LoginClient"] = client.Id;
			controller.IndexOffice(string.Empty);
			Assert.IsFalse(controller.Context.Response.WasRedirected);
			var ipPool = new IpPool {
				Begin = Convert.ToUInt32(ipAdress.Address - 1),
				End = Convert.ToUInt32(ipAdress.Address + 1)
			};
			var switchNew = new NetworkSwitch("test", session.Query<Zone>().First());
			session.Save(switchNew);
			session.Save(ipPool);
			var endPoint = new ClientEndpoint(client, 2, switchNew);
			session.Save(endPoint);
			var lease = new Lease(endPoint) {
				Pool = ipPool,
				Ip = ipAdress
			};
			session.Save(lease);
			session.Flush();

			Request.UserHostAddress = addresses.Last();
			client.FirstLunch = false;
			session.Save(client);
			session.Flush();
			controller.IndexOffice(string.Empty);
			Assert.IsFalse(controller.Context.Response.WasRedirected);

			Request.UserHostAddress = addresses.Last();
			client.FirstLunch = false;
			session.Save(client);
			ipPool.End = Convert.ToUInt32(ipAdress.Address + 100000000);
			session.Save(ipPool);
			session.Flush();
			controller.IndexOffice(string.Empty);
			Assert.IsTrue(controller.Context.Response.WasRedirected);

			Request.UserHostAddress = addresses.First();
			client.FirstLunch = false;
			ipPool.End = Convert.ToUInt32(ipAdress.Address + 1);
			session.Save(ipPool);
			session.Save(client);
			session.Flush();
			controller.IndexOffice(string.Empty);
			Assert.IsTrue(controller.Context.Response.WasRedirected);
		}

		private List<string> GenerateIp()
		{
			var part = new Random().Next(250);
			return new List<string> { string.Format("{0}.{0}.{0}.{0}", part), string.Format("{0}.{0}.{0}.{1}", part, part + 5) };
		}

		[Test]
		public void Write_off_for_channel_group_activation()
		{
			var channel = new ChannelGroup("Тестовый пакет каналов 1", 100, 10) { ActivationCost = 20 };
			session.Save(channel);

			Request.Params.Add("iptv.Channels[0].Id", channel.Id.ToString());
			((StubRequest)Request).HttpMethod = "POST";
			controller.Services();

			Assert.That(client.Iptv.Channels.Count, Is.EqualTo(1));
			Assert.That(client.UserWriteOffs.Count, Is.EqualTo(1));
			Assert.That(client.UserWriteOffs[0].Sum, Is.EqualTo(20));
		}

		[Test]
		public void Write_off_after_tariff_change()
		{
			var oldTariff = client.PhysicalClient.Tariff.Name;
			var tarriff = new Tariff("Тариф для тестирования изменения тарифов", 500);
			session.Save(new TariffChangeRule(client.PhysicalClient.Tariff, tarriff, 50));
			session.Save(tarriff);

			Request.Params.Add("client.PhysicalClient.Id", client.PhysicalClient.Id.ToString());
			Request.Params.Add("client.PhysicalClient.Tariff.Id", tarriff.Id.ToString());
			((StubRequest)Request).HttpMethod = "POST";
			controller.Services();

			Assert.That(client.PhysicalClient.Tariff, Is.EqualTo(tarriff));
			Assert.That(client.UserWriteOffs.Count, Is.EqualTo(1));
			Assert.That(client.UserWriteOffs[0].Sum, Is.EqualTo(50));
			Assert.That(client.UserWriteOffs[0].Comment, Is.EqualTo(string.Format("Изменение тарифа, старый '{0}' новый 'Тариф для тестирования изменения тарифов'", oldTariff)));
		}

		[Test]
		public void First_visit_if_have_endpoint()
		{
			var networkSwitch = new NetworkSwitch { Name = "testFirstVisit" };
			var lease = new Lease { Port = 5, Ip = IPAddress.Parse("192.168.0.25"), Switch = networkSwitch };
			var endpoint = new ClientEndpoint(client, 5, networkSwitch);
			session.Save(networkSwitch);
			session.Save(lease);
			session.Save(endpoint);

			((StubRequest)Request).HttpMethod = "POST";
			controller.FirstVisit(client.PhysicalClient.Id);

			client = session.Get<Client>(client.Id);
			Assert.AreEqual(client.Endpoints.Count, 0);

			session.Delete(endpoint);
			session.Flush();

			((StubRequest)Request).HttpMethod = "POST";
			controller.FirstVisit(client.PhysicalClient.Id);
			var id = client.Id;
			TearDown();
			Init();
			var client2 = session.Query<Client>().First(c => c.Id == id);
			Assert.AreEqual(client2.Endpoints.Count, 1);
			var paymentForConnect = session.Query<PaymentForConnect>().Where(p => p.EndPoint == client2.Endpoints[0]).FirstOrDefault();
			Assert.AreEqual(paymentForConnect.Sum, 555);
		}
	}
}