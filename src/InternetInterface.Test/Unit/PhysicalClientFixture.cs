using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class PhysicalClientFixture
	{
		private PhysicalClients client;

		[SetUp]
		public void Setup()
		{
			client = new PhysicalClients {
				Street = "Студенческая",
				House = 12,
				CaseHouse = "а",
				Apartment = 100,
				Client = new Client()
			};
		}

		[Test]
		public void Get_cut_address()
		{
			Assert.That(client.GetCutAdress(), Is.EqualTo("ул. Студенческая д. 12 Корп а кв. 100"));
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
	}
}