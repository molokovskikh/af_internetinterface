using InternetInterface.Controllers;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class PhysicalClientFixture
	{
		private PhysicalClient client;

		[SetUp]
		public void Setup()
		{
			client = new PhysicalClient {
				Street = "Студенческая",
				House = 12,
				CaseHouse = "а",
				Apartment = 100,
			};
			var baseClient = new Client(client, Settings.UnitTestSettings());
			client.Client.PhysicalClient = client;
			client.Client.Status = new Status {
				Blocked = true,
				Connected = false
			};
		}

		[Test]
		public void Get_cut_address()
		{
			Assert.That(client.GetShortAdress(), Is.EqualTo("ул. Студенческая д. 12 Корп а кв. 100"));
		}

		[Test]
		public void Write_off_with_prefered_type()
		{
			client.Balance = 1000;
			client.MoneyBalance = 600;
			client.VirtualBalance = 400;
			client.WriteOff(1500, false);
			Assert.That(client.Balance, Is.EqualTo(-500));
			Assert.That(client.VirtualBalance, Is.EqualTo(0));
			Assert.That(client.MoneyBalance, Is.EqualTo(-500));
		}

		[Test]
		public void First_write_virtual()
		{
			client.Balance = 1000;
			client.MoneyBalance = 600;
			client.VirtualBalance = 400;
			client.WriteOff(300);
			Assert.That(client.Balance, Is.EqualTo(700));
			Assert.That(client.VirtualBalance, Is.EqualTo(100));
			Assert.That(client.MoneyBalance, Is.EqualTo(600));
			client.WriteOff(300);
			Assert.That(client.Balance, Is.EqualTo(400));
			Assert.That(client.VirtualBalance, Is.EqualTo(0));
			Assert.That(client.MoneyBalance, Is.EqualTo(400));
		}

		[Test]
		public void Self_registration()
		{
			var lease = new Lease {
				Switch = new NetworkSwitch()
			};
			client.Tariff = new Tariff("Test", 100);
			client.Client.SelfRegistration(lease, new Status { Connected = true });
			Assert.That(client.Client.Payments.Count, Is.EqualTo(1));
			Assert.That(client.Client.Payments[0].Sum, Is.GreaterThan(0));
		}
	}
}