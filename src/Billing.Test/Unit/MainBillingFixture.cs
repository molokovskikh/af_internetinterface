#define BILLING_TEST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Controllers.Filter;
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

            new Partner
            {
                Login = "Test",
            }.Save();



            SessionScope.Current.Flush();

            InithializeContent.GetAdministrator = () => Partner.FindFirst();

			new Status
			{
				Blocked = false,
				Id = (uint)StatusType.Worked,
				Name = "unblocked"
			}.Save();

			new Status
			{
				Blocked = true,
				Id = (uint)StatusType.NoWorked,
				Name = "testBlockedStatus"
			}.Save();

			new InternetSettings{NextBillingDate = DateTime.Now}.Save();
		}

		public Clients CreateClient()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 590);
			client.Save();
			return client;
		}

		private void SetClientDate(Clients client, Interval rd)
		{
			client = Clients.FindFirst();
			client.RatedPeriodDate = rd.dtFrom;
			client.Update();
			SystemTime.Now = () => rd.dtTo;
			//billing.DtNow = rd.dtTo;
			billing.Compute();
		}

        [Test]
        public void MaxDebtTest()
        {
            var client = CreateClient();
            client.Disabled = false;
            SystemTime.Reset();
            var dayInMonth = DateTime.DaysInMonth(SystemTime.Now().AddDays(-15).Year, SystemTime.Now().AddDays(-15).Month);
            client.RatedPeriodDate = SystemTime.Now().AddDays(-15);
            client.Update();
            billing.Compute();
            var spisD0 = WriteOff.Queryable.FirstOrDefault(w => w.Client == client);
            client.Refresh();
            Console.WriteLine("Interval DO: " + client.GetInterval());
            Assert.That(dayInMonth, Is.EqualTo(client.GetInterval()));
            client.DebtDays = 29;
            client.Update();
            billing.Compute();
            var slisD29 = WriteOff.Queryable.Where(w => w.Client == client).ToList().LastOrDefault();
            client.Refresh();
            Console.WriteLine("Interval D29: " + client.GetInterval());
            Assert.That(dayInMonth + 29, Is.EqualTo(client.GetInterval()));
            Console.WriteLine(string.Format("spisDO: {0}  spisD29: {1}", spisD0.WriteOffSum.ToString("0.00"), slisD29.WriteOffSum.ToString("0.00")));
        }

        [Test]
        public void CanBlockTest()
        {
            var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, -1000);
            client.Disabled = false;
            client.Save();
            Assert.IsTrue(client.CanBlock());
            client.DebtWork = true;
            client.Update();
            Assert.IsFalse(client.CanBlock());
            client.DebtWork = false;
            client.PostponedPayment = DateTime.Now;
            SystemTime.Now = () => DateTime.Now;
            client.Update();
            Assert.IsFalse(client.CanBlock());
            SystemTime.Now = () => DateTime.Now.AddDays(2);
            client.Update();
            Assert.IsTrue(client.CanBlock());
            client.DebtWork = true;
            client.Update();
            Assert.IsFalse(client.CanBlock());
            client.DebtWork = false;
            client.PostponedPayment = null;
            client.Update();
            Assert.IsTrue(client.CanBlock());
            client.Disabled = true;
            client.Update();
            Assert.IsFalse(client.CanBlock());
        }

	    [Test]
        public void Test1151()
        {
            var client = CreateClient();
            client.Disabled = false;
            client.RatedPeriodDate = new DateTime(2011, 5, 31, 15, 05, 23);
            SystemTime.Now = () => new DateTime(2011, 6, 30, 22, 02, 03);
            billing.Compute();
            Console.WriteLine("WriteOffSum "+WriteOff.Queryable.Where(w => w.Client == client).ToList().Last().WriteOffSum.ToString("0.00"));
            client.Refresh();
            Console.WriteLine("RatedDate " + client.RatedPeriodDate.Value.ToShortDateString());
            Console.WriteLine("Interval " + client.GetInterval());
            Console.WriteLine("DebtDays " + client.DebtDays);
            Assert.That(client.DebtDays, Is.EqualTo(1));
            SystemTime.Now = () => new DateTime(2011, 7, 31 , 19, 03, 6);
            billing.Compute();
            Console.WriteLine("WriteOffSum " + WriteOff.Queryable.Where(w => w.Client == client).ToList().Last().WriteOffSum.ToString("0.00"));
            client.Refresh();
            Console.WriteLine("RatedDate " + client.RatedPeriodDate.Value.ToShortDateString());
            Console.WriteLine("Interval " + client.GetInterval());
            Console.WriteLine("DebtDays " + client.DebtDays);
            Assert.That(client.DebtDays, Is.EqualTo(0));
        }

        [Test]
        public void TetsDebtDays()
        {
            var client = CreateClient();
            client.Disabled = false;
            client.RatedPeriodDate = new DateTime(2011, 5, 15, 15, 05, 23);
            SystemTime.Now = () => new DateTime(2011, 6, 15, 22, 02, 03);
            billing.Compute();
            client.Refresh();
            Assert.That(client.DebtDays, Is.EqualTo(0));
            Assert.That(((DateTime)client.RatedPeriodDate).Date, Is.EqualTo(new DateTime(2011, 6, 15)));
        }

        [Test]
        public void FindDebt()
        {
           var count = 0;
           var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 500000);
            client.Disabled = false;
            client.RatedPeriodDate = new DateTime(2011, 6, 9, 15, 00, 9);
            client.Save();
            var tarif = client.PhysicalClient.Tariff;
            tarif.Price = 500;
            tarif.Update();
            while (client.DebtDays < 1 && count < 365)
            {
                Console.WriteLine("Count: " + count + " Date: " + SystemTime.Now().Date);
                SystemTime.Now = () => new DateTime(2011, 6, 7, 22, 15, 9).AddDays(count);
                Console.WriteLine("IterDate " + SystemTime.Now().ToShortDateString());
                billing.Compute();
                client.Refresh();
                count++;
                Console.WriteLine("Rated Date: " + client.RatedPeriodDate);
                Console.WriteLine("DateNow : " + SystemTime.Now());
                Console.WriteLine("-------------------------------------------------");
            }
            Console.WriteLine("***********************************");
            Console.WriteLine("All iterations " + count);
            Console.WriteLine("DebtDays " + client.DebtDays);
            Console.WriteLine("Rated Date: " + client.RatedPeriodDate);
            Console.WriteLine("DateNow : " + SystemTime.Now());
            Assert.That(365 , Is.EqualTo(count));
        }

        [Test]
        public void ShowBalWarning()
        {
            var client = BaseBillingFixture.CreateAndSaveClient("ShowClient", false, 300);
            client.BeginWork = DateTime.Now;
            client.RatedPeriodDate = DateTime.Now;
            client.Save();
            var partBalance = client.GetPrice() / client.GetInterval();
            client.PhysicalClient.Balance = partBalance * 2 - 1;
            client.Update();
            billing.Compute();
            client.Refresh();
            Assert.IsTrue(client.ShowBalanceWarningPage);
            Assert.Greater(client.PhysicalClient.Balance, 0);
            new Payment {
                            Client = client, 
                            Sum = partBalance,
                            BillingAccount = false,
                        }.Save();
            billing.On();
            client.Refresh();
            Assert.IsFalse(client.ShowBalanceWarningPage);
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
        public void PostponedPayment()
        {
            var client_Post = BaseBillingFixture.CreateAndSaveClient("testRated", false, 100);
            client_Post.RatedPeriodDate = DateTime.Now;
            client_Post.Save();
            var client_simple = BaseBillingFixture.CreateAndSaveClient("testRated", false, 100);
            client_simple.Save();
            var pclient_post = client_Post.PhysicalClient;
            pclient_post.Balance -= 200;
            pclient_post.Update();

            billing.Compute();

            client_simple.Refresh();
            client_Post.Refresh();
            Assert.IsTrue(client_Post.Disabled);
            Assert.IsFalse(client_simple.Disabled);

            client_Post.PostponedPayment = DateTime.Now;
            client_Post.Disabled = false;
            client_Post.Update();

            billing.Compute();
            billing.On();

            client_simple.Refresh();
            client_Post.Refresh();
            Assert.IsFalse(client_Post.Disabled);
            Assert.IsFalse(client_simple.Disabled);

            SystemTime.Now = () => DateTime.Now.AddHours(25);

            //billing.Compute();
            billing.On();

            client_simple.Refresh();
            client_Post.Refresh();
            Assert.IsTrue(client_Post.Disabled);
            Assert.IsFalse(client_simple.Disabled);

            client_Post.Disabled = false;
            client_Post.Update();

            billing.Compute();

            client_simple.Refresh();
            client_Post.Refresh();
            Assert.IsTrue(client_Post.Disabled);
            Assert.IsFalse(client_simple.Disabled);

            new Payment {
                            Client = client_Post,
                            Sum = 1000,
                            BillingAccount = false
                        }.Save();

            billing.On();

            client_Post.Refresh();

            Assert.IsNull(client_Post.PostponedPayment);
        }

	    [Test]
        public void RatedTest()
        {
            var client = BaseBillingFixture.CreateAndSaveClient("testRated", false, 100);
            client.RatedPeriodDate = SystemTime.Now().AddMonths(-1);
            client.Save();
            billing.Compute();
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
            lPerson.Save();
			var client = new Clients {
				Disabled = false,
				Name = "TestLawyer",
				ShowBalanceWarningPage = false,
                LawyerPerson = lPerson
			};
			client.Save();

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
			client.Update();
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

        [Test]
        public void sdf()
        {
            Console.WriteLine(new DateTime(2011, 08, 31).AddMonths(1).ToShortDateString());
            Console.WriteLine((new DateTime(2011, 08, 31).AddMonths(1) - new DateTime(2011, 08, 31)).Days);
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
				client.Update();
				for (int i = 0; i < date.Count-1; i++)
				{
					SetClientDate(client, date[i]);
					Assert.That(date[i+1].GetInterval(), Is.EqualTo(client.GetInterval()));
					Console.WriteLine(string.Format("Между датами {0} прошло {1} дней", date[i], date[i].GetInterval()));
				}
			}
            
            //client.DeleteAndFlush();
		}
	}
}
