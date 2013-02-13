using System;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class PaymentFixture : global::Test.Support.Web.WatinFixture2
	{
		[Test]
		public void ProcessPaymentTest()
		{
			Open("Payments/ProcessPayments");
			Assert.That(browser.Text, Is.StringContaining("Загрузка выписки"));
		}

		[Test]
		public void NewTest()
		{
			Open("Payments/New");
			browser.Button("addPayment").Click();
			Assert.That(browser.Text, Is.StringContaining("Значение должно быть больше нуля"));
		}

		[Test]
		public void IndexTest()
		{
			Open("Payments/Index");
			browser.Button(Find.ByValue("Показать")).Click();
			Assert.That(browser.Text, Is.StringContaining("История платежей"));
		}

		[Test]
		public void PaymentCommentTest()
		{
			var client = ClientHelper.Client();
			Save(client);
			Open("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", client.Id);
			browser.TextField("BalanceText").AppendText("100");
			browser.TextField("CommentText").AppendText("testComment");
			browser.Button("ChangeBalanceButton").Click();
			AssertText("Платеж ожидает обработки");
			Click("Платежи");
			AssertText("testComment");
		}

		[Test]
		public void Cancel_client_payment()
		{
			var client = ClientHelper.Client();
			var payment = new Payment(client, 1000);
			Save(client, payment);

			Open("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", client.Id);
			Click("Платежи");
			Css("#paymentReason").TypeText("тест");
			Click("#SearchResults", "Отменить");
			AssertText("Отменено");

			session.Clear();
			payment = session.Get<Payment>(payment.Id);
			Assert.That(payment, Is.Null);
		}

		[Test]
		public void only_inforoom_test()
		{
			var recipient = new Recipient { Name = "testRecipient" };
			var client = ClientHelper.Client();
			client.Recipient = recipient;
			var bankPayment = new BankPayment(client, DateTime.Now, 300) { Recipient = recipient };
			var newClient = ClientHelper.Client();
			newClient.Recipient = recipient;
			var payment = new Payment(client, 300) { BankPayment = bankPayment };
			Save(client, newClient, bankPayment, payment, recipient);

			Open("Payments/Edit?id=" + bankPayment.Id);
			Click("Сохранить");

			AssertText("Получатель платежей может быть только Инфорум");
		}

		[Test]
		public void Change_payer_bank_payment()
		{
			var recipient = new Recipient { Name = "testRecipient", BankAccountNumber = "40702810602000758601" };
			var client = ClientHelper.Client();
			client.Recipient = recipient;
			var bankPayment = new BankPayment(client, DateTime.Now, 300) { Recipient = recipient };
			var newClient = ClientHelper.Client();
			newClient.Recipient = recipient;
			var payment = new Payment(client, 300) { BankPayment = bankPayment };
			Save(client, newClient, bankPayment, payment, recipient);
			var paymentId = payment.Id;

			Open("Payments/Edit?id=" + bankPayment.Id);

			browser.TextField("payment_payer_id").Value = newClient.Id.ToString();
			Click("Сохранить");

			Close();

			Assert.IsNull(session.Get<Payment>(paymentId));
		}
	}
}