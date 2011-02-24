using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using NUnit.Framework;
using InternetInterface.Models;
using Billing;

namespace Billing.Test.Unit
{

	[TestFixture]
	class MainBillingFixture
	{
		//private static Clients client;

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
			MainBilling.DtNow = rd.dtTo;
			MainBilling.Compute();
		}

		[Test]
		public void Write_off()
		{
			var client = Clients.FindFirst();
			//var client = CreateClient();
			client.PhisicalClient.Balance = Tariff.FindFirst().Price.ToString();
			client.UpdateAndFlush();
			new Status
				{
					Blocked = true,
					Name = "testBlockedStatus"
				}.SaveAndFlush();
			var interval = new Interval("15.01.2011", "15.02.2011");
			var dayCount = interval.GetInterval();
			interval.dtTo = DateTime.Parse("15.01.2011");
			for (var i = 0; i < dayCount; i++)
			{
				interval.dtTo = interval.dtTo.AddDays(1);
				SetClientDate(client, interval);
			}
			Assert.That(Math.Round(Convert.ToDecimal(client.PhisicalClient.Balance), 2), Is.EqualTo(0.00));
		}

		/// <summary>
		/// Следить за состоянием client.DebtDays;
		/// </summary>
		[Test]
		public void IntervalTest()
		{
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
			            			}
			            	};

			foreach (var date in dates)
			{
				client.DebtDays = 0;
				client.UpdateAndFlush();
				for (int i = 0; i < date.Count - 1; i++)
				{
					SetClientDate(client, date[i]);
					Assert.That(date[i + 1].GetInterval(), Is.EqualTo(client.GetInterval()));
					Console.WriteLine(string.Format("Между датами {0} прошло {1} дней", date[i], date[i].GetInterval()));
				}
			}
		}
	}
}
