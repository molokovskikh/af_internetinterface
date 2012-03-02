using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			_client.PhysicalClient.Balance = 100000;
			_client.RatedPeriodDate = DateTime.Now;
			_client.StartNoBlock = DateTime.Now;
			_client.Update();
			SystemTime.Reset();
		}

		[Test]
		public void Creates_sales()
		{
			for (int i = 1; i < 20; i++) {
				SystemTime.Now = () => DateTime.Now.AddMonths(i).AddDays(-5);
				billing.Compute();
				var sale = 0m;
				if (i > PerionCount)
					sale = MinSale + (i - MinSale - 1)*SaleStep;
				if (sale > MaxSale)
					sale = MaxSale;
				Assert.AreEqual(sale, _client.Sale);
			}
		}

		[Test]
		public void Sale_sum()
		{
			SystemTime.Now = () => DateTime.Now.AddMonths(4).AddDays(-5);
			billing.Compute();
			Assert.AreEqual(_client.Sale, SaleStep*MinSale);
			var writeOff = WriteOff.FindFirst();
			var preSaleSum = _client.PhysicalClient.Tariff.Price / _client.GetInterval();
			Assert.AreEqual(Math.Round(writeOff.WriteOffSum, 5), Math.Round(preSaleSum - preSaleSum*(SaleStep*MinSale / 100), 5));
			Assert.AreEqual(writeOff.Sale, SaleStep*MinSale);
		}

		[Test]
		public void On_Off_Sale()
		{
			SystemTime.Now = () => DateTime.Now.AddMonths(4).AddDays(-5);
			_client.PhysicalClient.Balance = 100;
			_client.Update();
			billing.Compute();
			Assert.AreEqual(_client.Sale, SaleStep*MinSale);
			while (!_client.Disabled) {
				billing.Compute();
				billing.OnMethod();
				Console.WriteLine(_client.PhysicalClient.Balance);
			}
			Assert.AreEqual(_client.Sale, 0);
			new Payment {
				Client = _client,
				Sum = 1000
			}.Save();
			billing.OnMethod();
			billing.Compute();
			Assert.AreEqual(_client.Sale, 0);
			_client.StartNoBlock = SystemTime.Now();
			billing.Compute();
			Assert.AreEqual(_client.Sale, 0);
			SystemTime.Now = () => DateTime.Now.AddMonths(8).AddDays(-10);
			billing.Compute();
			Assert.AreEqual(_client.Sale, SaleStep*MinSale);
		}

		[Test]
		public void Set_start_date()
		{
			_client.PhysicalClient.Balance = 100;
			_client.Update();
			var iterationCount = 0;
			while (!_client.Disabled) {
				billing.Compute();
				iterationCount ++;
			}
			Assert.Greater(iterationCount, 1);
			Assert.IsNull(_client.StartNoBlock);
			billing.Compute();
			Assert.IsNull(_client.StartNoBlock);
			var writeOffs = WriteOff.Queryable.ToList();
			Assert.That(writeOffs.Sum(off => off.Sale), Is.EqualTo(0));
			Assert.Greater(writeOffs.Count, 0);
			new Payment {
				Sum = _client.GetPriceForTariff(),
				Client = _client
			}.Save();
			billing.OnMethod();
			_client.RatedPeriodDate = DateTime.Now;
			_client.Update();
			billing.Compute();
			Assert.That(_client.StartNoBlock.Value.Date, Is.EqualTo(SystemTime.Now().Date));
		}
	}
} 