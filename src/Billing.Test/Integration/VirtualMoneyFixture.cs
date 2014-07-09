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
		[SetUp]
		public void SetUpFixture()
		{
			InitSession();
		}

		[Test]
		public void MiniMumMoneyTest()
		{
			var payment = new Payment(client, 50);
			var virtualPayment = new Payment(client, 30) { Virtual = true };
			session.Save(payment);
			session.Save(virtualPayment);
			client.PhysicalClient.Balance = 0;
			session.Update(client);

			billing.OnMethod();

			var userWriteOff = new UserWriteOff(client, 10, "test");
			session.Save(userWriteOff);

			billing.OnMethod();

			session.Refresh(client);
			Assert.AreEqual(client.Balance, 70m);
			Assert.AreEqual(client.PhysicalClient.VirtualBalance, 30);
			Assert.AreEqual(client.PhysicalClient.MoneyBalance, 40);
		}

		[Test]
		public void MiniMumVirtualTest()
		{
			var payment = new Payment(client, 50);
			var virtualPayment = new Payment(client, 30) { Virtual = true };
			session.Save(payment);
			session.Save(virtualPayment);
			client.PhysicalClient.Balance = 0;
			session.Update(client);

			billing.OnMethod();

			var userWriteOff = new UserWriteOff(client, 65, "test");
			session.Save(userWriteOff);

			billing.OnMethod();

			session.Refresh(client);
			Assert.AreEqual(client.Balance, 15);
			Assert.AreEqual(client.PhysicalClient.VirtualBalance, 15);
			Assert.AreEqual(client.PhysicalClient.MoneyBalance, 0);
		}

		[Test]
		public void BaseVirtualTest()
		{
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
			Assert.That(writeOff.WriteOffSum, Is.EqualTo(writeOff.MoneySum));
			client.Refresh();
			Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(paySum + 5));
			Assert.That(client.PhysicalClient.MoneyBalance, Is.EqualTo(100 - paySum));

			billing.Compute();

			session.Refresh(client);
			writeOff = client.WriteOffs.Last();
			Assert.That(writeOff.WriteOffSum, Is.EqualTo(paySum));
			Assert.That(writeOff.VirtualSum, Is.EqualTo(0));
			Assert.That(writeOff.MoneySum, Is.EqualTo(paySum));
			client.Refresh();
			Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(paySum + 5));
			Assert.That(client.PhysicalClient.MoneyBalance, Is.EqualTo(100 - paySum * 2));

			billing.Compute();

			session.Refresh(client);
			writeOff = client.WriteOffs.Last();
			Assert.That(writeOff.WriteOffSum, Is.EqualTo(paySum));
			Assert.That(writeOff.VirtualSum, Is.EqualTo(0));
			Assert.That(writeOff.MoneySum, Is.EqualTo(paySum));
			client.Refresh();
			Assert.That(client.PhysicalClient.VirtualBalance, Is.EqualTo(paySum + 5));
		}
	}
}