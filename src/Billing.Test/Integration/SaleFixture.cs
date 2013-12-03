using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	public class SaleFixture : MainBillingFixture
	{
		[SetUp]
		public void SetupThis()
		{
			using (new SessionScope()) {
				client.PhysicalClient.Balance = 100000;
				client.RatedPeriodDate = DateTime.Now;
				client.StartNoBlock = DateTime.Now;
				client.Update();
			}
		}

		[Test]
		public void Creates_sales()
		{
			for (int i = 1; i < 20; i++) {
				SystemTime.Now = () => DateTime.Now.AddMonths(i).AddDays(-5);
				billing.Compute();
				var sale = 0m;
				if (i > PerionCount)
					sale = MinSale + (i - MinSale - 1) * SaleStep;
				if (sale > MaxSale)
					sale = MaxSale;
				using (new SessionScope()) {
					client.Refresh();
					Assert.AreEqual(sale, client.Sale);
				}
			}
		}

		[Test]
		public void Sale_sum()
		{
			SystemTime.Now = () => DateTime.Now.AddMonths(4).AddDays(-5);
			billing.Compute();
			using (new SessionScope()) {
				client.Refresh();
				Assert.AreEqual(client.Sale, SaleStep * MinSale);
				var writeOff = WriteOff.FindFirst();
				var preSaleSum = client.PhysicalClient.Tariff.Price / client.GetInterval();
				Assert.AreEqual(Math.Round(writeOff.WriteOffSum, 2), Math.Round(preSaleSum - preSaleSum * (SaleStep * MinSale / 100), 2));
				Assert.AreEqual(writeOff.Sale, SaleStep * MinSale);
			}
		}

		[Test]
		public void On_Off_Sale()
		{
			SystemTime.Now = () => DateTime.Now.AddMonths(4).AddDays(-5);
			using (new SessionScope()) {
				client.PhysicalClient.Balance = 100;
				client.Update();
			}
			billing.Compute();
			using (new SessionScope()) {
				client.Refresh();
				Assert.AreEqual(client.Sale, SaleStep * MinSale);
			}
			Wait(client, () => client.Disabled, () => {
				billing.Compute();
				billing.OnMethod();
			});
			Assert.AreEqual(client.Sale, 0);
			new Payment {
				Client = client,
				Sum = 1000
			}.Save();
			billing.OnMethod();
			billing.Compute();
			using (new SessionScope()) {
				client.Refresh();
				Assert.AreEqual(client.Sale, 0);
				client.StartNoBlock = SystemTime.Now();
				client.Update();
			}
			billing.Compute();
			using (new SessionScope())
				client.Refresh();
			Assert.AreEqual(client.Sale, 0);
			SystemTime.Now = () => DateTime.Now.AddMonths(8).AddDays(-10);
			billing.Compute();
			using (new SessionScope())
				client.Refresh();
			Assert.AreEqual(client.Sale, SaleStep * MinSale);
		}

		[Test]
		public void Set_start_date()
		{
			using (new SessionScope()) {
				client.PhysicalClient.Balance = 100;
				client.PhysicalClient.MoneyBalance = 100;
				client.PhysicalClient.VirtualBalance = 0;
				client.PhysicalClient.Save();
			}
			var iterationCount = Wait(client, () => client.Disabled, () => billing.Compute());
			Assert.Greater(iterationCount, 1);
			using (new SessionScope())
				client.Refresh();
			Assert.IsNull(client.StartNoBlock);
			billing.Compute();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsNull(client.StartNoBlock);
				var writeOffs = WriteOff.Queryable.ToList();
				Assert.That(writeOffs.Sum(off => off.Sale), Is.EqualTo(0));
				Assert.Greater(writeOffs.Count, 0);
				new Payment {
					Sum = client.GetPriceForTariff(),
					Client = client
				}.Save();
			}
			billing.OnMethod();
			using (new SessionScope()) {
				client.Refresh();
				client.RatedPeriodDate = DateTime.Now;
				client.Update();
			}
			billing.Compute();
			using (new SessionScope())
				client.Refresh();
			Assert.That(client.StartNoBlock.Value.Date, Is.EqualTo(SystemTime.Now().Date));
		}
	}
}