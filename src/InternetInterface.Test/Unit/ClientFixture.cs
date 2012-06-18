using InternetInterface.Models;
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
		[Test]
		public void Auto_unblock_only_if_service_not_forbide_auto_unblocking()
		{
			var client = new Client();
			client.PhysicalClient = new PhysicalClient();
			client.PhysicalClient.Tariff = new Tariff("Тестовый тариф", 100);
			client.Payments.Add(new Payment(client, 1000));
			Assert.That(client.HavePaymentToStart(), Is.True);
			client.ClientServices.Add(new ClientService(client, new TestService()) { Activated = true });
			Assert.That(client.HavePaymentToStart(), Is.False);
		}
	}
}