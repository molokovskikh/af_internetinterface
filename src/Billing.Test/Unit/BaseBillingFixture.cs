using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Unit
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

		public static Clients CreateAndSaveClient(string name, bool statusBlocked, decimal balance)
		{
			var phisicalClient = CreatePhisicalClient(statusBlocked, balance);
			phisicalClient.SaveAndFlush();
			return new Clients
			       	{
			       		Disabled = false,
			       		FirstLease = true,
			       		DebtDays = 0,
			       		Name = name,
			       		PhisicalClient = phisicalClient
			       	};
		}

		public static PhisicalClients CreatePhisicalClient(bool statusBlocked, decimal balance)
		{
			var tariff = CreateTariff((int)balance);
			tariff.SaveAndFlush();
			var status = CreateStatus(statusBlocked);
			status.SaveAndFlush();
			return new PhisicalClients
			       	{
			       		Name = "TestPhisicalClient",
						AutoUnblocked = true,
						Balance = balance.ToString(),
						Connected = true,
						Tariff = tariff,
						Status = status 
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

		public static Status CreateStatus(bool blocked)
		{
			return new Status
			       	{
			       		Blocked = blocked,
						Name = "testStatus"
			       	};
		}
	}
}
