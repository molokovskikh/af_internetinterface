using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
				MainBillingFixture.CleanDb();
				new Payment().Save();
			}
		}

		[Test]
		public void BaseMutexFixture()
		{
			var thread = new Thread(_billing.SafeProcessPayments);
			thread.Start();
			thread.Join();
			_billing.Run();
		}

		[Test]
		public void NoWriteOff()
		{
			var thread = new Thread(() => {
				_billing.SafeProcessPayments();
				Thread.Sleep(500);
			});

			thread.Start();
			thread.Join();

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