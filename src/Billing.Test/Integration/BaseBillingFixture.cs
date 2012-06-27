using System;
using System.Linq;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using InternetInterface.Models;
using InternetInterface.Models.Services;
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
			Internet internet;
			IpTv iptv;
			using(new SessionScope()) {
				internet = ActiveRecordLinqBase<Internet>.Queryable.First();
				iptv = ActiveRecordLinqBase<IpTv>.Queryable.First();
			}

			var phisicalClient = CreatePhisicalClient(statusBlocked, balance);
			phisicalClient.Save();
			var client = new Client {
				Disabled = false,
				Sale = 0,
				DebtDays = 0,
				Name = name,
				PhysicalClient = phisicalClient,
				BeginWork = DateTime.Now ,
				RatedPeriodDate = DateTime.Now,
				YearCycleDate = DateTime.Now
			};
			//тк некоторые тесты не вызывают метод активации
			client.ClientServices.Add(new ClientService(client, internet, true) {
				Activated = true
			});
			client.ClientServices.Add(new ClientService(client, iptv) {
				Activated = true
			});
			return client;
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
				Client = Client.Queryable.Count(c => c.Name== "testblockedClient") != 0 ? Client.Queryable.First(c => c.Name== "testblockedClient") : Client.FindFirst(),
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
