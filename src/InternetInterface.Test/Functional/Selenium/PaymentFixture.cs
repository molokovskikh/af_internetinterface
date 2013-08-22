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

		[SetUp]
		public void SetUp()
		{
			bankPayment = SavePayment(client);

			newClient = ClientHelper.Client();
			newClient.Recipient = bankPayment.Recipient;
			Save(client, newClient, bankPayment, bankPayment.Payment);
		}

		[Test]
		public void Move_payment()
		{
			Css("#show_payments").Click();
			WaitForVisibleCss("#SearchResults");
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
