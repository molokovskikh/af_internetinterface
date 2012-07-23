using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class PaymentFixture
	{
		private Client client;
		private PhysicalClients physicalClient;

		[SetUp]
		public void Setup()
		{
			client = new Client {
				PhysicalClient = new PhysicalClients()
			};
			physicalClient = client.PhysicalClient;
			physicalClient.Client = client;

			InitializeContent.GetAdministrator = () => new Partner();
		}

		[Test]
		public void Cancel_payment()
		{
			var payment = new Payment(client, 1000) {
				BillingAccount = true
			};
			physicalClient.Balance += payment.Sum;
			physicalClient.MoneyBalance += payment.Sum;

			var message = payment.Cancel("Comment");
			Assert.That(physicalClient.Balance, Is.EqualTo(0));
			Assert.That(physicalClient.MoneyBalance, Is.EqualTo(0));
			Assert.That(message.Appeal, Is.EqualTo("Удален платеж на сумму 1\u00A0000,00р. \r\n Комментарий: Comment"));
		}

		[Test]
		public void Cancel_money_correctly()
		{
			physicalClient.Balance = 1500;
			physicalClient.MoneyBalance = 1000;
			physicalClient.VirtualBalance = 500;

			var payment = new Payment(client, 500) {
				BillingAccount = true
			};
			payment.Cancel(string.Empty);
			Assert.That(physicalClient.Balance, Is.EqualTo(1000));
			Assert.That(physicalClient.MoneyBalance, Is.EqualTo(500));
			Assert.That(physicalClient.VirtualBalance, Is.EqualTo(500));
		}
	}
}