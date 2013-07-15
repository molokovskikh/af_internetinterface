using System;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Functional.Selenium
{
	[TestFixture]
	public class PaymentFixture : AcceptanceFixture
	{
		private Client newClient;
		private BankPayment bankPayment;
		private Payment payment;

		[SetUp]
		public void SetUp()
		{
			var recipient = new Recipient { Name = "testRecipient", BankAccountNumber = "40702810602000758601" };
			session.Save(recipient);
			client.Recipient = recipient;
			bankPayment = new BankPayment(client, DateTime.Now, 300) { Recipient = recipient };
			newClient = ClientHelper.Client();
			newClient.Recipient = recipient;
			payment = new Payment(client, 300) { BankPayment = bankPayment };
			Save(client, newClient, bankPayment, payment);
		}

		[Test]
		public void Move_payment()
		{
			Css("#show_payments").Click();

			ClickButton("#SearchResults", "Переместить");
			Css(".ui-dialog #action_Comment").SendKeys("тестовое перемещение");
			Css(".ui-dialog .term").SendKeys(newClient.Id.ToString());
			ClickButton(".ui-dialog", "Найти");
			WaitForCss(".ui-dialog .search-editor-v2 select");
			var selectedValue = Css(".ui-dialog .search-editor-v2 select").SelectedOption.GetAttribute("value");
			Assert.AreEqual(newClient.Id.ToString(), selectedValue);

			ClickButton(".ui-dialog", "Сохранить");
			AssertText("Перемещен");

			session.Refresh(client);
			session.Refresh(newClient);
			Assert.AreEqual(0, client.Payments.Sum(p => p.Sum));
			Assert.AreEqual(300, newClient.Payments.Sum(p => p.Sum));
		}
	}
}
