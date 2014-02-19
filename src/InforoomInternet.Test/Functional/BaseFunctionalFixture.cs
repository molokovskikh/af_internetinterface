using System.Net;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InforoomInternet.Test.Functional
{
	[TestFixture]
	class BaseFunctionalFixture : SeleniumFixture
	{
		protected Lease Lease;
		protected ClientEndpoint ClientEndpoint;
		protected Client Client;
		protected IpPool Pool;
		protected PhysicalClient PhysicalClient;
		protected Internet Internet;
		protected IpTv IpTv;
		protected Tariff Tariff;

		[SetUp]
		public void SetUp()
		{
			Pool = new IpPool {
				IsGray = true,
				Begin = IPAddress.Parse("192.168.1.1").ToBigEndian(),
				End = IPAddress.Parse("192.168.1.100").ToBigEndian(),
			};
			PhysicalClient = new PhysicalClient();
			Client = new Client();
			Client.PhysicalClient = PhysicalClient;
			ClientEndpoint = new ClientEndpoint();
			ClientEndpoint.Client = Client;
			Lease = new Lease(ClientEndpoint);
			Lease.Ip = IPAddress.Parse("192.168.1.1");
			Lease.Pool = Pool;
			Internet = new Internet { HumanName = "internet" };
			IpTv = new IpTv { HumanName = "iptv" };
			Tariff = new Tariff("testTariff", 100);
			session.SaveMany(Internet, IpTv, Tariff);
			Client.ClientServices.Add(new ClientService(Client, Internet));
			Client.ClientServices.Add(new ClientService(Client, IpTv));
			PhysicalClient.Tariff = Tariff;
			session.SaveMany(Pool, PhysicalClient, Client, ClientEndpoint, Lease);
		}

		[TearDown]
		public void TearDown()
		{
			session.DeleteMany(Lease, ClientEndpoint, Client, PhysicalClient, Pool, Tariff, IpTv, Internet);
		}
	}
}
