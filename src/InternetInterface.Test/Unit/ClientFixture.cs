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

		[Test]
		public void Auto_unblock_only_if_service_not_forbide_auto_unblocking()
		{
			client.Payments.Add(new Payment(client, 1000));
			Assert.That(client.HavePaymentToStart(), Is.True);
			client.ClientServices.Add(new ClientService(client, new TestService()) { Activated = true });
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
	}
}