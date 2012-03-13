using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using WatiN.Core;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	class PaymentFixture : WatinFixture
	{
		[Test]
		public void ProcessPaymentTest()
		{
			using (var browser = Open("Payments/ProcessPayments"))
			{
				Thread.Sleep(1000);
				Assert.That(browser.Text, Is.StringContaining("Загрузка выписки"));
			}
		}

		[Test]
		public void NewTest()
		{
			using (var browser = Open("Payments/New"))
			{
				browser.Button("addPayment").Click();
				Assert.That(browser.Text, Is.StringContaining("Значение должно быть больше нуля"));
			}
		}

		[Test]
		public void IndexTest()
		{
			using (var browser = Open("Payments/Index"))
			{
				browser.Button(Find.ByValue("Показать")).Click();
				Assert.That(browser.Text, Is.StringContaining("История платежей"));
			}
		}
	}
}
