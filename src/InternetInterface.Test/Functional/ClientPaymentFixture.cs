using System;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	public class ClientPaymentFixture : RoleBasedFixture
	{
		private Client client;
		private PhysicalClient physicalClient;
		private ClientEndpoint endpoint;
		
		protected override int GetRoleId()
		{
			return 1;
		}

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
			defaultUrl =
				string.Format("UserInfo/ShowPhysicalClient?filter.ClientCode={0}&filter.EditingConnect=true&filter.Editing={1}",
					client.Id, false);
		}

		[Test]
		public void CheckPayment()
		{
			Open(client.Redirect());
			AssertText("Пополнить баланс клиента");
			Css("#BalanceText").SendKeys("900");
			Css("#CommentText").SendKeys("Оплата на 900");
			Css("#ChangeBalanceButton").Click();
			Css("#show_payments").Click();
			WaitForText(CurrentPartner.Name);
			AssertText(CurrentPartner.Name);
		}
	}
}