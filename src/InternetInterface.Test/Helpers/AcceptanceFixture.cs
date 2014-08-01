using System;
using System.Linq;
using Common.Tools.Calendar;
using InternetInterface.Models;
using InternetInterface.Services;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
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

		protected IWebElement SafeSelectService(string name)
		{
			Click("Управление услугами");
			WaitAnimation();

			var service = session.Query<Service>().First(s => s.HumanName == name);
			var el = browser.FindElementByCssSelector(String.Format("input[name='serviceId'][value='{0}']", service.Id));
			var form = el.FindElement(By.XPath(".."));
			var findElement = form.FindElement(By.CssSelector("button"));
			if (!findElement.Displayed) {
				Click(name);
				WaitAnimation();
			}
			return form;
		}

		protected void WaitAnimation()
		{
			new WebDriverWait(browser, 5.Second())
				.Until(d => Convert.ToInt32(Eval("return $(\":animated\").length")) == 0);
		}
	}
}
