using System;
using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	public class Interval
	{
		public DateTime dtFrom;
		public DateTime dtTo;

		public Interval(string _dtFrom, string _dtTo)
		{
			dtTo = Convert.ToDateTime(_dtTo);
			dtFrom = Convert.ToDateTime(_dtFrom);
		}

		public decimal GetInterval()
		{
			return (dtTo - dtFrom).Days;
		}

		public override string ToString()
		{
			return string.Format("{0} и {1}", dtFrom.ToShortDateString(), dtTo.ToShortDateString());
		}
	}

	public class BaseBillingFixture
	{
		public static Client CreateAndSaveClient(string name, bool statusBlocked, decimal balance)
		{
			Service[] defaultServices;
			using (new SessionScope()) {
				defaultServices = DefaultServices().ToArray();
			}

			var phisicalClient = CreatePhisicalClient(statusBlocked, balance);
			phisicalClient.Save();

			var client = new Client(phisicalClient, defaultServices) {
				Disabled = false,
				Sale = 0,
				DebtDays = 0,
				FreeBlockDays = 0,
				PercentBalance = 0,
				Name = name,
				BeginWork = DateTime.Now,
				RatedPeriodDate = DateTime.Now,
				YearCycleDate = DateTime.Now
			};
			client.PhysicalClient.ConnectSum = 0;
			client.Internet.ActivatedByUser = true;
			//тк некоторые тесты не вызывают метод активации
			foreach (var service in client.ClientServices)
				service.Activate();

			return client;
		}

		public static IEnumerable<Service> DefaultServices()
		{
			return new Service[] {
				ActiveRecordLinqBase<Internet>.Queryable.First(),
				ActiveRecordLinqBase<IpTv>.Queryable.First(),
			};
		}

		public static PhysicalClient CreatePhisicalClient(bool statusBlocked, decimal balance)
		{
			var tariff = CreateTariff((int)balance);
			tariff.Save();
			return new PhysicalClient {
				Name = "TestPhisicalClient",
				Balance = balance,
				Tariff = tariff,
			};
		}

		public static Tariff CreateTariff(int balanceForTariff)
		{
			return new Tariff {
				Name = "testTariff",
				Price = balanceForTariff,
				Description = "testTariff"
			};
		}

		public static void CreatePayment(decimal sum)
		{
			new Payment {
				BillingAccount = false,
				Client = Client.Queryable.Count(c => c.Name == "testblockedClient") != 0 ? Client.Queryable.First(c => c.Name == "testblockedClient") : Client.FindFirst(),
				PaidOn = DateTime.Now,
				RecievedOn = DateTime.Now,
				Sum = sum
			}.SaveAndFlush();
		}

		public static void CreateAndSaveInternetSettings()
		{
			new InternetSettings {
				NextBillingDate = DateTime.Now.AddHours(-1)
			}.SaveAndFlush();
		}
	}
}
