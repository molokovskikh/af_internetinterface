#define TEST

using System;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using NUnit.Framework;
using InternetInterface.Models;

namespace Billing.Test.Unit
{

	[TestFixture]
	public class MainBillingFixture
	{
		private MainBilling billing;

		public MainBillingFixture()
		{
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

			new InternetSettings{NextBillingDate = DateTime.Now}.SaveAndFlush();
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
            CreateClient();
		    var unblockedClient = Clients.FindFirst();
		    unblockedClient.AutoUnblocked = true;
            unblockedClient.Update();
		    var phisClient = unblockedClient.PhysicalClient;
		    phisClient.Balance = -100;
            phisClient.Update();
            billing.Compute();
            Assert.IsTrue(unblockedClient.Status.Blocked);
		    new Payment {
		                    Client = unblockedClient,
                            Sum = 200
		                }.Save();
            billing.On();
            Assert.IsFalse(unblockedClient.Status.Blocked);
			Assert.That(unblockedClient.PhysicalClient.Balance, Is.EqualTo(100));
		}

		[Test]
		public void LawyerPersonTest()
		{
		    SystemTime.Reset();
            var lPerson = new LawyerPerson
            {
                Balance = 0,
                Tariff = 10000m,
            };
            lPerson.SaveAndFlush();
			var client = new Clients {
				Disabled = false,
				Name = "TestLawyer",
				ShowBalanceWarningPage = false,
                LawyerPerson = lPerson
			};
			client.SaveAndFlush();

            for (int i = 0; i < DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month) * 2; i++)
            {
                billing.Compute();
            }
            Console.WriteLine(lPerson.Balance);
			Assert.That( -19999m, Is.GreaterThan(lPerson.Balance));
			Assert.That(-20000m, Is.LessThan(lPerson.Balance));
			billing.On();
			Assert.IsTrue(client.ShowBalanceWarningPage);
			Console.WriteLine(client.ShowBalanceWarningPage);
			lPerson.Balance += 1000;
			billing.On();
			Assert.IsTrue(!client.ShowBalanceWarningPage);
			Console.WriteLine(client.ShowBalanceWarningPage);
		}

		[Test]
		public void Write_off()
		{
			BaseBillingFixture.CreateAndSaveInternetSettings();
			CreateClient();
			var client = Clients.FindFirst();
			//var client = CreateClient();
			client.PhysicalClient.Balance = Tariff.FindFirst().Price;
			client.UpdateAndFlush();
			var interval = new Interval("15.01.2011", "15.02.2011");
			var dayCount = interval.GetInterval();
			interval.dtTo = DateTime.Parse("15.01.2011");
			for (var i = 0; i < dayCount; i++)
			{
				interval.dtTo = interval.dtTo.AddDays(1);
				SetClientDate(client, interval);
			}
			Assert.That(Math.Round(Convert.ToDecimal(client.PhysicalClient.Balance), 2), Is.LessThan(0.00));
			Console.WriteLine("End balance = " + Math.Round(Convert.ToDecimal(client.PhysicalClient.Balance), 2));
			var writeOffs = WriteOff.FindAll();
			//Assert.That(writeOffs.Length, Is.EqualTo(31));
			foreach (var writeOff in writeOffs)
			{
				Console.WriteLine(string.Format("id = {0} date = {1} sum = {2}", writeOff.Id, writeOff.WriteOffDate.ToShortDateString(), Math.Round(writeOff.WriteOffSum,2)));
			}
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

		[Test]
		public void Complex_tariff()
		{
			var tariff = new Tariff {
				Price = 200,
				FinalPriceInterval = 1,
				FinalPrice = 400,
			};
			var client = new Clients {
				Name = "TestLawyer",
				BeginWork = DateTime.Now.AddMonths(-1),
				RatedPeriodDate = DateTime.Now,
				PhysicalClient = new PhysicalClients {
					Tariff = tariff,
					Balance = 1000
				}
			};
			client.Save();
			client.PhysicalClient.Save();
			tariff.Save();
			billing.Compute();
			var writeOffs = WriteOff.Queryable.Where(w => w.Client == client).ToList();
			Assert.That(writeOffs.Count, Is.EqualTo(1));
			Assert.That(writeOffs[0].WriteOffSum, Is.GreaterThan(10));
		}

		/// <summary>
		/// Следить за состоянием client.DebtDays;
		/// </summary>
		[Test]
		public void IntervalTest()
		{
			BaseBillingFixture.CreateAndSaveInternetSettings();
		    foreach (var writeOff in WriteOff.FindAll())
		    {
		        writeOff.Delete();
		    }
		    foreach (var clientse in Clients.FindAll())
		    {
		        clientse.Delete();   
		    }
			var client = CreateClient();

			var dates = new List<List<Interval>> {
				new List<Interval> {
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
				new List<Interval> {
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
				new List<Interval> {
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
