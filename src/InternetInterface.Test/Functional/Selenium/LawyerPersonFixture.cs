using System;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Functional.Selenium
{
	[TestFixture]
	public class LawyerPersonFixture : AcceptanceFixture
	{
		private Client laywerPerson;

		[SetUp]
		public void Setup()
		{
			laywerPerson = ClientHelper.CreateLaywerPerson();
			session.Save(laywerPerson);

			defaultUrl = laywerPerson.Redirect();
		}

		[Test]
		public void Move_payment()
		{
			var payment = SavePayment(laywerPerson);
			Save(payment, payment.Payment);

			Css("#show_payments").Click();

			ClickButton("#SearchResults", "Переместить");
			Css(".ui-dialog #action_Comment").SendKeys("тестовое перемещение");
			Css(".ui-dialog .term").SendKeys(client.Id.ToString());
			ClickButton(".ui-dialog", "Найти");
			WaitForCss(".ui-dialog .search-editor-v2 select");
			var selectedValue = Css(".ui-dialog .search-editor-v2 select").SelectedOption.GetAttribute("value");
			Assert.AreEqual(client.Id.ToString(), selectedValue);

			ClickButton(".ui-dialog", "Сохранить");

			session.Refresh(client);
			session.Refresh(laywerPerson);
			Assert.AreEqual(300, client.Payments.Sum(p => p.Sum));
			Assert.AreEqual(0, laywerPerson.Payments.Sum(p => p.Sum));
		}
	}
}