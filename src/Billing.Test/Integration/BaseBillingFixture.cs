using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using InternetInterface.Helpers;
using InternetInterface.Models;
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

	class BaseBillingFixture
	{
		[SetUp]
		public void BaseSetup()
		{
		}

		public static Client CreateAndSaveClient(string name, bool statusBlocked, decimal balance)
		{
			var phisicalClient = CreatePhisicalClient(statusBlocked, balance);
			phisicalClient.Save();
			return new Client
					{
						Disabled = false,
						//FirstLease = true,
						DebtDays = 0,
						Name = name,
						PhysicalClient = phisicalClient,
						BeginWork = DateTime.Now ,
						RatedPeriodDate = DateTime.Now,
						YearCycleDate = DateTime.Now
					};
		}

		public static PhysicalClients CreatePhisicalClient(bool statusBlocked, decimal balance)
		{
			var tariff = CreateTariff((int)balance);
			tariff.Save();
			return  new PhysicalClients
					{
						Name = "TestPhisicalClient",
						Balance = balance,
						Tariff = tariff,
					};
		}

		public static Tariff CreateTariff(int balanceForTariff)
		{
			return new Tariff
					{
						Name = "testTariff",
						Price = balanceForTariff
					};
		}

		public static void CreatePayment(decimal sum)
		{
			new Payment
				{
					BillingAccount = false,
					Client = Client.Queryable.Where(c => c.Name== "testblockedClient").Count() != 0 ? Client.Queryable.Where(c => c.Name== "testblockedClient").First() : Client.FindFirst(),
					PaidOn = DateTime.Now,
					RecievedOn = DateTime.Now,
					Sum = sum
				}.SaveAndFlush();
		}

		public static void CreateAndSaveInternetSettings()
		{
			new InternetSettings
				{
					NextBillingDate = DateTime.Now.AddHours(-1)
				}.SaveAndFlush();
		}
	}
}
