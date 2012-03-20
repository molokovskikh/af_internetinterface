using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BillingService;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Models;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class MutexFixture
	{
		private MainBilling _billing;

		[SetUp]
		public void Cleaner()
		{
			_billing = new MainBilling();
			using (new SessionScope()) {
				MainBillingFixture.PrepareTest();
				new Payment().Save();
			}
		}

		[Test]
		public void BaseMutexFixture()
		{
			var thread = new Thread(_billing.On);
			try {
				thread.Start();
			}
			finally {
				thread.Join();
			}
			_billing.Run();
		}

		[Test]
		public void NoWriteOff()
		{
			//Assert.Throws<NullReferenceException>(_billing.On);

			var thread = new Thread(() => {
				_billing.On();
				Thread.Sleep(500);
			});
			try
			{
				thread.Start();
			}
			catch { }
			finally {
				thread.Join();
			}

			using (new SessionScope()) {
				MainBillingFixture.CreateClient();
				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(0));
				var billingTime = InternetSettings.FindFirst();
				billingTime.NextBillingDate = DateTime.Now.AddHours(-1);
				billingTime.Save();
			}
			_billing.Run();
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(1));
			}
		}
	}
}
