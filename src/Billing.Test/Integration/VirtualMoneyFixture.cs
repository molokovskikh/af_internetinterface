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
			var paySum = _client.GetPrice() / _client.GetInterval();
			new Payment {
				Sum = 100,
				Client = _client
			}.Save();
			new Payment {
				Client = _client,
				Sum = paySum + 5,
				Virtual = true
			}.Save();

			using (new SessionScope()) {
				_client.PhysicalClient.Balance = 0;
				_client.Update();
			}

			billing.OnMethod();

			using (new SessionScope()) {
				_client.Refresh();
				Assert.That(_client.PhysicalClient.VirtualBalance, Is.EqualTo(Decimal.Round(paySum + 5, 5)));
				Assert.That(_client.PhysicalClient.Balance, Is.EqualTo(Decimal.Round(paySum + 5 + 100, 2)));
				Assert.That(_client.PhysicalClient.MoneyBalance, Is.EqualTo(100m));
			}

			billing.Compute();

			using (new SessionScope()) {
				var writeOff = WriteOff.ForClient(_client).FirstOrDefault();
				Assert.That(writeOff.WriteOffSum, Is.EqualTo(Decimal.Round(writeOff.VirtualSum, 2)));
				_client.Refresh();
				Assert.That(_client.PhysicalClient.VirtualBalance, Is.EqualTo(Decimal.Round(5, 5)));
				Assert.That(_client.PhysicalClient.MoneyBalance, Is.EqualTo(100));
			}

			billing.Compute();

			using (new SessionScope()) {
			var writeOff = WriteOff.ForClient(_client).Last();
				Assert.That(writeOff.WriteOffSum, Is.EqualTo(Decimal.Round(paySum, 2)));
				Assert.That(writeOff.VirtualSum, Is.EqualTo(Decimal.Round(5, 2)));
				Assert.That(writeOff.MoneySum, Is.EqualTo(Decimal.Round(paySum - 5, 5)));
				_client.Refresh();
				Assert.That(_client.PhysicalClient.VirtualBalance, Is.EqualTo(0));
				Assert.That(_client.PhysicalClient.MoneyBalance, Is.EqualTo(Decimal.Round(100 - paySum + 5, 5)));
			}

			billing.Compute();

			using (new SessionScope()) {
			var writeOff = WriteOff.ForClient(_client).Last();
				Assert.That(writeOff.WriteOffSum, Is.EqualTo(Decimal.Round(paySum, 2)));
				Assert.That(writeOff.VirtualSum, Is.EqualTo(Decimal.Round(0, 2)));
				Assert.That(writeOff.MoneySum, Is.EqualTo(Decimal.Round(paySum, 5)));
				_client.Refresh();
				Assert.That(_client.PhysicalClient.VirtualBalance, Is.EqualTo(0));
			}
		}
	}
}
