using System;
using System.ComponentModel;
using System.Net;
using Common.Tools;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	public class TestService : Service
	{
		public TestService()
		{
			BlockingAll = true;
		}
	}

	[TestFixture]
	public class ClientFixture
	{
		private Client client;
		private Settings settings;

		[SetUp]
		public void Setup()
		{
			settings = Settings.UnitTestSettings();
			client = new Client(new PhysicalClient(), settings);
			client.PhysicalClient.Tariff = new Tariff("Тестовый тариф", 100);
		}

		[TearDown]
		public void TearDown()
		{
			SystemTime.Reset();
		}

		[Test]
		public void Auto_unblock_only_if_service_not_forbide_auto_unblocking()
		{
			client.Payments.Add(new Payment(client, 1000));
			Assert.That(client.HavePaymentToStart(), Is.True);
			client.ClientServices.Add(new ClientService(client, new TestService()) { IsActivated = true });
			Assert.That(client.HavePaymentToStart(), Is.False);
		}

		[Test]
		public void Calculate_price_ignore_disable()
		{
			var price = client.GetPriceIgnoreDisabled();
			Assert.That(price, Is.EqualTo(100));
		}

		[Test]
		public void Activete_internet_by_default()
		{
			client = new Client(new PhysicalClient(), settings);
			Assert.That(client.ClientServices[0].ActivatedByUser, Is.True);
		}

		[Test]
		public void ShowWarningTest()
		{
			client.PhysicalClient.Balance = 300;
			client.RatedPeriodDate = DateTime.Now.AddMonths(-1);
			client.BeginWork = DateTime.Now.AddDays(-8);
			Assert.IsTrue(client.NeedShowWarning());
			client.PhysicalClient.PassportNumber = "123456";
			Assert.IsFalse(client.NeedShowWarning());
		}

		[Test]
		public void Write_off_for_static_ip()
		{
			client.BeginWork = DateTime.Now;
			var clientEndpoint = new ClientEndpoint(client, 1, new NetworkSwitch("тест", new Zone("тест"))) {
				Ip = IPAddress.Parse("91.209.124.67")
			};
			client.AddEndpoint(clientEndpoint, settings);
			client.ClientServices.Each(s => s.TryActivate());

			var price = client.GetPrice();
			//тариф + плата за фиксированный адрес
			Assert.AreEqual(100 + 30, price);

			client.FindActiveService<PinnedIp>().IsFree = true;
			Assert.AreEqual(100, client.GetPrice());
		}

		[Test]
		public void Calculate_block_date()
		{
			SystemTime.Now = () => new DateTime(2014, 5, 7, 9, 56, 13);
			client.RatedPeriodDate = DateTime.Now.AddMonths(-1);
			client.BeginWork = DateTime.Now.AddDays(-8);
			client.ClientServices.Each(s => s.TryActivate());

			client.PhysicalClient.Balance = 50;
			Assert.IsFalse(client.ShouldNotifyOnLowBalance());

			client.PhysicalClient.Balance = 6;
			Assert.IsTrue(client.ShouldNotifyOnLowBalance());
			Assert.AreEqual(new DateTime(2014, 5, 9), client.GetPossibleBlockDate());

			client.PhysicalClient.Balance = 1;
			Assert.AreEqual(new DateTime(2014, 5, 8), client.GetPossibleBlockDate());
		}

		[Test]
		public void Activate_duplicate_rent()
		{
			client.Activate(new ClientService(client, new IpTvBoxRent()) {
				SerialNumber = "1",
				Model = "2",
			});
			client.Activate(new ClientService(client, new IpTvBoxRent()) {
				SerialNumber = "3",
				Model = "2",
			});
		}

		[Test]
		public void Do_not_recompensate_rent_on_balance_below_zero()
		{
			SystemTime.Now = () => new DateTime(2014, 7, 29);
			client.BeginWork = SystemTime.Now();
			client.RatedPeriodDate = SystemTime.Now();
			client.PhysicalClient.Balance = 100;
			client.PhysicalClient.MoneyBalance = 100;
			client.Activate(new ClientService(client, new HardwareRent()) {
				RentableHardware = new RentableHardware(50, "Тестовое оборудование")
			});
			client.ClientServices.Each(s => s.TryActivate());
			Assert.AreEqual(100, client.GetPrice());

			DailyWriteoff();
			Assert.AreEqual(96.77, client.PhysicalClient.Balance);
			client.PhysicalClient.Balance = 0;
			client.PhysicalClient.MoneyBalance = 0;
			client.PhysicalClient.VirtualBalance = 0;
			DailyWriteoff();
			Assert.AreEqual(50, client.GetPrice());
			Assert.AreEqual(-4.84, client.PhysicalClient.Balance);
		}

		[Test]
		public void Activate_speed_boost()
		{
			var clientEndpoint = new ClientEndpoint(client, 1, new NetworkSwitch("тест", new Zone("тест")));
			client.AddEndpoint(clientEndpoint, settings);
			client.Activate(new ClientService(client, new SpeedBoost()) {
				BeginWorkDate = DateTime.Today,
				EndWorkDate = DateTime.Today.AddDays(1)
			});
			client.ClientServices.Each(s => s.TryActivate());
			Assert.AreEqual(23, client.Endpoints[0].PackageId);
			Assert.IsTrue(client.IsNeedRecofiguration);
		}

		private void DailyWriteoff()
		{
			client.PhysicalClient.WriteOff(client.GetSumForRegularWriteOff());
			if (client.CanBlock())
				client.SetStatus(new Status(StatusType.NoWorked));
			client.ClientServices.Each(s => s.WriteOffProcessed());
			client.ClientServices.Each(s => s.TryDeactivate());
		}
	}
}
