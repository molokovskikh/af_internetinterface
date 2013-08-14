using System;
using System.ComponentModel;
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

		[SetUp]
		public void Setup()
		{
			client = new Client();
			client.PhysicalClient = new PhysicalClient();
			client.PhysicalClient.Tariff = new Tariff("Тестовый тариф", 100);
			client.Activate(new ClientService(client, new Internet(), true));
			client.Activate(new ClientService(client, new IpTv()));
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
			var settings = Settings.UnitTestSettings();
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
	}
}