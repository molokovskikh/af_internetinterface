using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class VirtualMoneyFixture : MainBillingFixture
	{
		[Test]
		public void BaseVirtualTest()
		{
			var paySum = client.GetPrice() / client.GetInterval();
			new Payment {
				Sum = 100,
				Client = client
			}.Save();
			new Payment {
				Client = client,
				Sum = paySum + 5,
				Virtual = true
			}.Save();

			using (new SessionScope()) {
				client.PhysicalClient.Balance = 0;
				client.Update();
			}

			billing.OnMethod();

			using (new SessionScope()) {
				client.Refresh();
				Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(Decimal.Round(paySum + 5, 2)));
				Assert.That(client.PhysicalClient.Balance, Is.EqualTo(Decimal.Round(paySum + 5 + 100, 2)));
				Assert.That(client.PhysicalClient.MoneyBalance, Is.EqualTo(100m));
			}

			billing.Compute();

			using (new SessionScope()) {
				session.Refresh(client);
				var writeOff = client.WriteOffs.FirstOrDefault();
				Assert.That(writeOff.WriteOffSum, Is.EqualTo(Decimal.Round(writeOff.VirtualSum, 2)));
				client.Refresh();
				Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(Decimal.Round(5, 2)));
				Assert.That(client.PhysicalClient.MoneyBalance, Is.EqualTo(100));
			}

			billing.Compute();

			using (new SessionScope()) {
				session.Refresh(client);
				var writeOff = client.WriteOffs.Last();
				Assert.That(writeOff.WriteOffSum, Is.EqualTo(Decimal.Round(paySum, 2)));
				Assert.That(writeOff.VirtualSum, Is.EqualTo(Decimal.Round(5, 5)));
				Assert.That(writeOff.MoneySum, Is.EqualTo(Decimal.Round(paySum - 5, 5)));
				client.Refresh();
				Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(0));
				Assert.That(client.PhysicalClient.MoneyBalance, Is.EqualTo(Decimal.Round(100 - paySum + 5, 2)));
			}

			billing.Compute();

			using (new SessionScope()) {
				session.Refresh(client);
				var writeOff = client.WriteOffs.Last();
				Assert.That(writeOff.WriteOffSum, Is.EqualTo(Decimal.Round(paySum, 2)));
				Assert.That(writeOff.VirtualSum, Is.EqualTo(Decimal.Round(0, 5)));
				Assert.That(writeOff.MoneySum, Is.EqualTo(Decimal.Round(paySum, 5)));
				client.Refresh();
				Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(0));
			}
		}
	}
}