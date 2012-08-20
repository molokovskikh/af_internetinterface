using System;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

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
	}
}