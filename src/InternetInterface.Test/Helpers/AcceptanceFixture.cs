using System;
using InternetInterface.Models;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Helpers
{
	public class AcceptanceFixture : SeleniumFixture
	{
		public PhysicalClient physicalClient;
		public Client client;
		public ClientEndpoint endpoint;

		[SetUp]
		public void FixtureSetup()
		{
			client = ClientHelper.Client(session);
			physicalClient = client.PhysicalClient;

			session.Save(client);
			endpoint = new ClientEndpoint {
				Client = client,
			};
			session.Save(endpoint);
			defaultUrl = string.Format("UserInfo/SearchUserInfo?filter.ClientCode={0}&filter.EditingConnect=true&filter.Editing={1}", client.Id, false);
		}

		protected BankPayment SavePayment(Client payer)
		{
			var recipient = new Recipient { Name = "testRecipient", BankAccountNumber = "40702810602000758601" };
			session.Save(recipient);
			payer.Recipient = recipient;
			var bankPayment = new BankPayment(payer, DateTime.Now, 300) { Recipient = recipient };
			var payment = new Payment(payer, 300) { BankPayment = bankPayment };
			bankPayment.Payment = payment;
			return bankPayment;
		}
	}
}
