#define TEST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using NUnit.Framework;
using InternetInterface.Models;
using Billing;

namespace Billing.Test.Unit
{

	[TestFixture]
	class MainBillingFixture
	{
		//private static Clients client;
		private MainBilling billing;

		public MainBillingFixture()
		{
			var i = 0;
			Console.WriteLine(i++ + i++ + ++i + ++i);

			billing = new MainBilling();

			new Status
			{
				Blocked = false,
				Id = (uint)StatusType.Worked,
				Name = "unblocked"
			}.SaveAndFlush();

			new Status
			{
				Blocked = true,
				Id = (uint)StatusType.NoWorked,
				Name = "testBlockedStatus"
			}.SaveAndFlush();
		}

		public Clients CreateClient()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 590);
			client.SaveAndFlush();
			return client;
		}

		private void SetClientDate(Clients client, Interval rd)
		{
			client = Clients.FindFirst();
			client.RatedPeriodDate = rd.dtFrom;
			client.UpdateAndFlush();
			SystemTime.Now = () => rd.dtTo;
			//billing.DtNow = rd.dtTo;
			billing.Compute();
		}

		[Test]
		public void OnTest()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("testblockedClient", true, 1000);
			client.SaveAndFlush();

			BaseBillingFixture.CreatePayment();

			billing.On();
			var unblockedClient = Clients.FindAllByProperty("Name", "testblockedClient").First();
			Assert.That(unblockedClient.PhisicalClient.Status.Blocked , Is.EqualTo(false));
			Assert.That(unblockedClient.PhisicalClient.Balance, Is.EqualTo(1500));
		}

		[Test]
		public void Write_off()
		{
			BaseBillingFixture.CreateAndSaveInternetSettings();
			var client = Clients.FindFirst();
			//var client = CreateClient();
			client.PhisicalClient.Balance = Tariff.FindFirst().Price;
			client.UpdateAndFlush();
			var interval = new Interval("15.01.2011", "15.02.2011");
			var dayCount = interval.GetInterval();
			interval.dtTo = DateTime.Parse("15.01.2011");
			for (var i = 0; i < dayCount; i++)
			{
				interval.dtTo = interval.dtTo.AddDays(1);
				SetClientDate(client, interval);
			}
			Assert.That(Math.Round(Convert.ToDecimal(client.PhisicalClient.Balance), 2), Is.LessThan(0.00));
		}

		[Test]
		public void RealTest()
		{
			BaseBillingFixture.CreateAndSaveInternetSettings();
			var b = new MainBilling();
			b.Run();
			var thisSettings = InternetSettings.FindFirst().NextBillingDate;
			Assert.That(thisSettings.ToShortDateString(), Is.EqualTo(DateTime.Now.AddDays(1).ToShortDateString()));
		}

		/// <summary>
		/// Следить за состоянием client.DebtDays;
		/// </summary>
		[Test]
		public void IntervalTest()
		{
			BaseBillingFixture.CreateAndSaveInternetSettings();
			var client = CreateClient();

			var dates = new List<List<Interval>>
			            	{
			            		new List<Interval>
			            			{
										new Interval("30.09.2010", "30.10.2010"),
										new Interval("30.10.2010", "30.11.2010"),
			            				new Interval("30.11.2010", "30.12.2010"),
			            				new Interval("30.12.2010", "30.01.2011"),
			            				new Interval("30.01.2011", "28.02.2011"),
			            				new Interval("28.02.2011", "30.03.2011"),
			            				new Interval("30.03.2011", "30.04.2011"),
										new Interval("30.04.2011", "30.05.2011"),
										new Interval("30.05.2011", "30.06.2011"),
										new Interval("30.06.2011", "30.07.2011"),
			            			},
			            		new List<Interval>
			            			{

			            				new Interval("31.10.2010", "30.11.2010"),
			            				new Interval("30.11.2010", "31.12.2010"),
			            				new Interval("31.12.2010", "31.01.2011"),
			            				new Interval("31.01.2011", "28.02.2011"),
			            				new Interval("28.02.2011", "31.03.2011"),
			            				new Interval("31.03.2011", "30.04.2011"),
			            				new Interval("30.04.2011", "31.05.2011"),
										new Interval("31.05.2011", "30.06.2011"),
										new Interval("30.06.2011", "31.07.2011"),
			            			},
			            		new List<Interval>
			            			{
			            				new Interval("15.10.2010", "15.11.2010"),
			            				new Interval("15.11.2010", "15.12.2010"),
			            				new Interval("15.12.2010", "15.01.2011"),
			            				new Interval("15.01.2011", "15.02.2011"),
			            				new Interval("15.02.2011", "15.03.2011"),
										new Interval("15.03.2011", "15.04.2011"),
										new Interval("15.04.2011", "15.05.2011"),
										new Interval("15.05.2011", "15.06.2011"),
										new Interval("15.06.2011", "15.07.2011"),
			            			}
			            	};

			foreach (var date in dates)
			{
				client.DebtDays = 0;
				client.UpdateAndFlush();
				for (int i = 0; i < date.Count-1; i++)
				{
					SetClientDate(client, date[i]);
					Assert.That(date[i+1].GetInterval(), Is.EqualTo(client.GetInterval()));
					Console.WriteLine(string.Format("Между датами {0} прошло {1} дней", date[i], date[i].GetInterval()));
				}
			}
		}
	}
}
