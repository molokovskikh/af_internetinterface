using System;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class ClientFixture : SeleniumFixture
	{
		[SetUp]
		public void Setup()
		{
			var partner = session.Query<Partner>().First(p => p.Login == Environment.UserName);
			partner.ShowContractOfAgency = true;
		}

		[TearDown]
		public void TearDown()
		{
			var partner = session.Query<Partner>().First(p => p.Login == Environment.UserName);
			partner.ShowContractOfAgency = false;
		}

		[Test]
		public void Show_contract_of_agency()
		{
			var client = ClientHelper.Client(session);
			session.Save(client);
			Open(client.Redirect());
			AssertText("Информация по клиенту");
			Css("#BalanceText").SendKeys("500");
			Click("Пополнить баланс");

			client.Refresh();
			var payment = client.Payments.Last(p => p.Sum == 500);
			Css("#show_payments").Click();
			WaitForText(payment.Id.ToString());
			Click(payment.Id.ToString());
			AssertText("ДОГОВОР ПОРУЧЕНИЯ");
		}
	}
}