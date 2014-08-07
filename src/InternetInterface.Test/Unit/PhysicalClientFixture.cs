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
			client.Client.Status = new Status();
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
			Assert.That(client.VirtualBalance, Is.EqualTo(400));
			Assert.That(client.MoneyBalance, Is.EqualTo(300));
			client.WriteOff(400);
			Assert.That(client.Balance, Is.EqualTo(300));
			Assert.That(client.VirtualBalance, Is.EqualTo(300));
			Assert.That(client.MoneyBalance, Is.EqualTo(0));
		}

		[Test]
		public void Copy_client_id_if_external_client_id_mandatory()
		{
			client.Client.Id = 100;
			client.HouseObj = new House("Ленина", 1, new RegionHouse("Белгород") { IsExternalClientIdMandatory = true });
			client.AfterSave();
			Assert.AreEqual(100, client.ExternalClientId);
		}
	}
}