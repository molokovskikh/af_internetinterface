using System;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class InternetFixture
	{
		private Client client;
		private ClientService clientService;

		[SetUp]
		public void Setup()
		{
			client = new Client();
			client.PhysicalClient = new PhysicalClients();
			client.PhysicalClient.Client = client;
			client.PhysicalClient.Tariff = new Tariff("Тестовый тариф", 100);
			clientService = new ClientService(client, new Internet());
			client.Activate(clientService);
			client.PhysicalClient.Balance = 1000;
			client.PhysicalClient.MoneyBalance = 1000;
			client.BeginWork = new DateTime(2012, 06, 01);
			client.RatedPeriodDate = new DateTime(2012, 06, 01);
		}

		[Test]
		public void Do_not_write_off_if_user_disabled_internet()
		{
			clientService.ActivatedByUser = false;
			Assert.That(client.GetPrice(), Is.EqualTo(0));
		}
	}
}