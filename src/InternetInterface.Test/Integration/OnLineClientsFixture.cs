using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Common.Tools;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using InternetInterface.Services;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class OnLineClientsFixture : IntegrationFixture
	{
		public Lease Lease { get; set; }
		public ClientEndpoint Endpoint { get; set; }
		public Client Client { get; set; }
		public PhysicalClient PhusicalClient { get; set; }
		public LawyerPerson LawyerPerson { get; set; }
		public NetworkSwitch Switch { get; set; }
		public Zone Zone { get; set; }
		public OnLineFilter Filter { get; set; }

		[SetUp]
		public void SetUp()
		{
			Filter = new OnLineFilter();
			InitializeContent.GetAdministrator = () => new Partner { AccesedPartner = new List<string>() };
			PhusicalClient = new PhysicalClient {
				Name = "Test",
				Surname = "Physical",
				Patronymic = "Client"
			};
			session.Save(PhusicalClient);
			LawyerPerson = new LawyerPerson {
				Name = "Test Lawyer Person",
				Region = session.Query<RegionHouse>().First()
			};
			session.Save(LawyerPerson);
			Client = new Client(PhusicalClient, new List<Service>()) {
				Name = "TestClientOnLine",
				LawyerPerson = LawyerPerson
			};
			session.Save(Client);
			Zone = new Zone { Name = "TestZone" };
			session.Save(Zone);
			Switch = new NetworkSwitch("TestCommutator", Zone);
			session.Save(Switch);
			Endpoint = new ClientEndpoint(Client, 10, Switch);
			session.Save(Endpoint);
			Lease = new Lease(Endpoint) { Switch = Switch };
			session.Save(Lease);
			Flush();
		}

		[TearDown]
		public void Finish()
		{
			session.Delete(Lease);
			session.Delete(Endpoint);
			session.Delete(Zone);
			session.Delete(Client);
			session.Delete(LawyerPerson);
			session.Delete(PhusicalClient);
			Flush();
		}

		[Test]
		public void Base_find_test()
		{
			var result = Filter.Find(session);
			Assert.IsTrue(result.Select(r => r.Client).Contains(Convert.ToInt32(Client.Id)));
		}

		[Test]
		public void Find_by_text()
		{
			Filter.SearchText = "TestClientOnLine";
			Base_find_test();
		}

		[Test]
		public void Find_by_switch()
		{
			Filter.Switch = Switch;
			Base_find_test();
		}

		[Test]
		public void Find_by_zone()
		{
			Filter.Zone = Zone;
			Base_find_test();
		}

		public void Find_by_lawyer_person()
		{
			Filter.ClientType = ClientTypeAll.Lawyer;
			Base_find_test();
		}

		[Test]
		public void Search_by_ip()
		{
			var bytes = IPAddress.Parse("10.0.50.1").GetAddressBytes().Reverse().ToArray();
			Lease.Ip = BitConverter.ToUInt32(bytes, 0);
			session.SaveOrUpdate(Lease);
			Flush();

			Filter.SearchText = "10.0.50";
			Base_find_test();
		}

		[Test]
		public void Search_static_ip()
		{
			Endpoint.StaticIps.Add(new StaticIp(Endpoint, "192.168.0.1"));
			session.SaveOrUpdate(Endpoint);

			Filter.SearchText = "192.168.0.1";
			var result = Filter.FindStatic(session);
			Assert.That(result.Any(r => r.EndpointId == Endpoint.Id), result.Implode(r => r.EndpointId));
		}
	}
}