using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class ErrorFixture : MainBillingFixture
	{
		[Test]
		public void MemorableLoggerTest()
		{
			var Payment = new Payment {
				Sum = 50
			};
			Payment.Save();
			billing.On();
			billing.On();
			billing.On();
			Payment.Client = _client;
			Payment.Save();
			billing.On();
		}
	}
}
