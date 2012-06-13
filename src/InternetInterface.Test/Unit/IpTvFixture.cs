using System;
using System.Linq;
using Common.Tools;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class IpTvFixture
	{
		private Client client;

		[SetUp]
		public void Setup()
		{
			client = new Client();
			client.PhysicalClient = new PhysicalClients();
			client.PhysicalClient.Client = client;
			client.PhysicalClient.Tariff = new Tariff("Тестовый тариф", 100);
			client.Activate(new ClientService(client, new Internet(), true));
			client.PhysicalClient.Balance = 1000;
			client.PhysicalClient.MoneyBalance = 1000;
			client.BeginWork = new DateTime(2012, 06, 01);
			client.RatedPeriodDate = new DateTime(2012, 06, 01);
			client.Status = new Status{ShortName = "Worked"};
		}

		[Test]
		public void If_iptv_box_rent_active_without_iptv_service_writeoff_for_Rent()
		{
			Rent();

			var price = client.GetPrice();
			Assert.That(price, Is.EqualTo(200));
		}

		[Test]
		public void Iptv_box_rent_with_iptv_writeoff_only_for_iptv()
		{
			Rent();
			IpTv();

			var price = client.GetPrice();
			Assert.That(price, Is.EqualTo(200), "100 за iptv 100 за интернет, амина бесплатно");
		}

		[Test]
		public void Iptv_only_charge_more_then_iptv_with_internet()
		{
			client.ClientServices.Remove(client.ClientServices.First(c => c.Service is Internet));
			IpTv();

			Assert.That(client.GetPrice(), Is.EqualTo(300));
		}

		[Test]
		public void While_voluntary_blocking_free_do_not_charge_for_rent()
		{
			Rent();
			IpTv();

			client.Activate(new ClientService(client, new VoluntaryBlockin{BlockingAll = true}) {BeginWorkDate = new DateTime(2012, 06, 01), Activated = true});
			client.FreeBlockDays = 30;
			Assert.That(client.GetPrice(), Is.EqualTo(0));
			client.FreeBlockDays = 0;
			Assert.That(client.GetPrice(), Is.EqualTo(190));
		}

		[Test]
		public void Free_rent_with_virtual_payment_writeoff()
		{
			var rent = Rent();
			rent.Service.Price = 300;
			IpTv();

			rent.WriteOff();

			Assert.That(client.PhysicalClient.Balance, Is.EqualTo(1000));
			Assert.That(client.PhysicalClient.MoneyBalance, Is.EqualTo(990));
			Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(10));
		}

		[Test]
		public void Do_not_write_off_if_user_disabled_iptv()
		{
			var iptv = IpTv();
			iptv.ActivatedByUser = false;
			Assert.That(client.GetPrice(), Is.EqualTo(100));
		}

		[Test]
		public void If_work_in_debt_activated_charge_for_rent()
		{
			Rent();
			var iptv = IpTv();
			client.PhysicalClient.WriteOff(1500);
			client.Disabled = true;
			iptv.Deactivate();
			Assert.That(client.GetPrice(), Is.EqualTo(100));
		}

		[Test]
		public void Charge_for_rent_if_user_deactivat_iptv()
		{
			Rent();
			var iptv = IpTv();
			iptv.ActivatedByUser = false;

			Assert.That(client.GetPrice(), Is.EqualTo(100));
		}

		private ClientService IpTv()
		{
			var clientService = new ClientService(client, new IpTv(new ChannelGroup("Тестовый пакет", 300, 100)), true);
			client.Activate(clientService);
			return clientService;
		}

		private ClientService Rent()
		{
			var clientService = new ClientService(client, new IpTvBoxRent(100));
			client.Activate(clientService);
			return clientService;
		}
	}
}