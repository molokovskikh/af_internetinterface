using System;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class PaymentFixture : AcceptanceFixture
	{
		private Client newClient;
		private BankPayment bankPayment;

		[SetUp]
		public void SetUp()
		{
			bankPayment = SavePayment(client);
			newClient = ClientHelper.Client(session);
			newClient.Recipient = bankPayment.Recipient;

			Save(client, newClient, bankPayment, bankPayment.Payment);
		}

		[Test]
		public void Move_payment()
		{
			Css("#show_payments").Click();
			WaitAnimation();
			ClickButton("#SearchResults", "Переместить");
			Css(".ui-dialog #action_Comment").SendKeys("тестовое перемещение");
			Css(".ui-dialog .term").SendKeys(newClient.Id.ToString());
			ClickButton(".ui-dialog", "Найти");
			WaitForVisibleCss(".ui-dialog .search-editor-v2 select");
			var selectedValue = Css(".ui-dialog .search-editor-v2 select").SelectedOption.GetAttribute("value");
			Assert.AreEqual(newClient.Id.ToString(), selectedValue);

			ClickButton(".ui-dialog", "Сохранить");
			AssertText("Перемещен");

			session.Refresh(client);
			session.Refresh(newClient);
			Assert.AreEqual(0, client.Payments.Sum(p => p.Sum));
			Assert.AreEqual(300, newClient.Payments.Sum(p => p.Sum));
		}

		[Test]
		public void ProcessPaymentTest()
		{
			Open("Payments/ProcessPayments");
			AssertText("Загрузка выписки");
		}

		[Test]
		public void NewTest()
		{
			Open("Payments/New");
			Css("#addPayment").Click();
			AssertText("Значение должно быть больше нуля");
		}

		[Test]
		public void IndexTest()
		{
			Open("Payments/Index");
			Click("Показать");
			AssertText("История платежей");
		}

		[Test]
		public void Cancel_client_payment()
		{
			var client = ClientHelper.Client(session);
			var payment = new Payment(client, 1000);
			Save(client, payment);

			Open("UserInfo/SearchUserInfo?filter.ClientCode={0}", client.Id);
			Css("#show_payments").Click();
			Click("Отменить");
			WaitForVisibleCss("#cancel_payment_action #comment");
			Css("#cancel_payment_action #comment").SendKeys("тест");
			Click(Css("#cancel_payment_action"), "Продолжить");
			AssertText("Отменено");

			session.Clear();
			payment = session.Get<Payment>(payment.Id);
			Assert.That(payment, Is.Null);
		}

		[Test]
		public void only_inforoom_test()
		{
			var recipient = client.Recipient;
			recipient.BankAccountNumber = string.Empty;
			Save(recipient);
			Close();

			Open("Payments/Edit?id=" + bankPayment.Id);
			Click("Сохранить");

			AssertText("Получатель платежей может быть только Инфорум");
		}

		[Test]
		public void Change_payer_bank_payment()
		{
			var paymentId = bankPayment.Payment.Id;

			Open("Payments/Edit?id=" + bankPayment.Id);
			RunJavaScript("return $('#payment_payer_id').val('" + newClient.Id + "');");
			Click("Сохранить");

			session.Clear();
			Assert.IsNull(session.Get<Payment>(paymentId));
		}

		[Test]
		public void New_payment_without_payer()
		{
			var unknownpayment = new BankPayment {
				Recipient = bankPayment.Recipient,
				Sum = 400,
				PayedOn = DateTime.Now,
				RegistredOn = DateTime.Now
			};
			session.Save(unknownpayment);

			Open("Payments/New");
			AssertText("Новый платеж");
		}
	}
}
