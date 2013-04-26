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
			InitSession();
			var paySum = Math.Round(client.GetPrice() / client.GetInterval(), 2);
			new Payment {
				Sum = 100,
				Client = client
			}.Save();
			new Payment {
				Client = client,
				Sum = paySum + 5,
				Virtual = true
			}.Save();

			client.PhysicalClient.Balance = 0;
			client.Update();
			billing.OnMethod();

			session.Refresh(client);
			Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(paySum + 5));
			Assert.That(client.PhysicalClient.Balance, Is.EqualTo(paySum + 5 + 100));
			Assert.That(client.PhysicalClient.MoneyBalance, Is.EqualTo(100m));

			billing.Compute();

			session.Refresh(client);
			var writeOff = client.WriteOffs.FirstOrDefault();
			Assert.That(writeOff.WriteOffSum, Is.EqualTo(writeOff.VirtualSum));
			client.Refresh();
			Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(5));
			Assert.That(client.PhysicalClient.MoneyBalance, Is.EqualTo(100));

			billing.Compute();

			session.Refresh(client);
			writeOff = client.WriteOffs.Last();
			Assert.That(writeOff.WriteOffSum, Is.EqualTo(paySum));
			Assert.That(writeOff.VirtualSum, Is.EqualTo(5));
			Assert.That(writeOff.MoneySum, Is.EqualTo(paySum - 5));
			client.Refresh();
			Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(0));
			Assert.That(client.PhysicalClient.MoneyBalance, Is.EqualTo(100 - paySum + 5));

			billing.Compute();

			session.Refresh(client);
			writeOff = client.WriteOffs.Last();
			Assert.That(writeOff.WriteOffSum, Is.EqualTo(paySum));
			Assert.That(writeOff.VirtualSum, Is.EqualTo(0));
			Assert.That(writeOff.MoneySum, Is.EqualTo(paySum));
			client.Refresh();
			Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(0));
		}
	}
}