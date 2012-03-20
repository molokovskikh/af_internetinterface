using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class WorkBillingFixture : MainBillingFixture
	{
		[Test]
		public void Base_work_system()
		{
			uint clientId;
			using (new SessionScope()) {
				var client = CreateClient();
				client.PhysicalClient.Tariff = null;
				client.Save();
				clientId = client.Id;
				var settings = ActiveRecordMediator<InternetSettings>.FindFirst();
				settings.NextBillingDate = DateTime.Now;
				settings.Save();
			}
			var _billing = new MainBilling();
			_billing.Run();
			using (new SessionScope()) {
				var settings = ActiveRecordMediator<InternetSettings>.FindFirst();
				Assert.IsTrue(settings.LastStartFail);
				var failClient = ActiveRecordMediator<Client>.FindByPrimaryKey(clientId);
				Assert.IsFalse(failClient.PaidDay);
				Assert.IsTrue(ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id).PaidDay);
				failClient.PhysicalClient.Tariff = ActiveRecordMediator<Tariff>.FindFirst();
				failClient.Save();
			}
			Thread.Sleep(2000);
			_billing.Run();
			using (new SessionScope()) {
				var settings = ActiveRecordMediator<InternetSettings>.FindFirst();
				Assert.False(settings.LastStartFail);
				var dn = DateTime.Now;
				Assert.That(settings.NextBillingDate, Is.EqualTo(new DateTime(dn.Year, dn.Month, dn.Day, 22, 0 ,0)));
				var failClient = ActiveRecordMediator<Client>.FindByPrimaryKey(clientId);
				Assert.IsTrue(failClient.PaidDay);
			}
		}
	}
}
