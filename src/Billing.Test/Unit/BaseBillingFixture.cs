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
			CreateAndSavePaymentForConnect(phisicalClient);
			return new Clients
			       	{
			       		Disabled = false,
			       		FirstLease = true,
			       		DebtDays = 0,
			       		Name = name,
			       		PhysicalClient = phisicalClient,
			       		SayBillingIsNewClient = true
			       	};
		}

		public static void CreateAndSavePaymentForConnect(PhysicalClients pclient)
		{
			new PaymentForConnect
				{
					ClientId = pclient,
					Summ = 200.ToString()
				}.SaveAndFlush();
		}

		public static PhysicalClients CreatePhisicalClient(bool statusBlocked, decimal balance)
		{
			var tariff = CreateTariff((int)balance);
			tariff.SaveAndFlush();
			//var status = CreateStatus(statusBlocked);
			//status.SaveAndFlush();
			return  new PhysicalClients
			       	{
			       		Name = "TestPhisicalClient",
						AutoUnblocked = true,
						Balance = balance,
						Connected = true,
						Tariff = tariff,
						Status = Status.Find((uint)StatusType.Worked)
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

		/*public static Status CreateStatus(bool blocked)
		{
			return new Status
			       	{
			       		Blocked = blocked,
						Name = "testStatus",
						Id = 7
			       	};
		}*/

		public static void CreatePayment(decimal sum)
		{
			new Payment
				{
					BillingAccount = false,
					Client = Clients.Queryable.Where(c => c.Name== "testblockedClient").Count() != 0 ? Clients.Queryable.Where(c => c.Name== "testblockedClient").First().PhysicalClient : PhysicalClients.FindFirst(),
					PaidOn = DateTime.Now,
					RecievedOn = DateTime.Now,
					Sum = sum.ToString()
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
