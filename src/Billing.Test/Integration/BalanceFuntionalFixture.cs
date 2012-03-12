using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	public class BalanceFuntionalFixture : MainBillingFixture
	{
		[Test]
		public void TestBillingDate()
		{
			var set = InternetSettings.FindFirst();
			set.NextBillingDate = new DateTime(2011, 9, 30, 22, 00, 00);
			set.SaveAndFlush();
			SystemTime.Now = () => new DateTime(2011, 9, 30, 22, 10, 00);
			billing.Run();
			set.Refresh();
			Assert.That(set.NextBillingDate, Is.EqualTo(new DateTime(2011, 10, 1, 22, 00, 00)));
			SystemTime.Reset();
		}

		[Test]
		public void TariffTest()
		{
			var client = CreateClient();
			var intervalTariff = new Tariff {
				FinalPrice = 200,
				FinalPriceInterval = 2,
				Price = 100,
				Name = "intervalTariff",
				Description = "intervalTariff"
			};
			intervalTariff.Save();
			client.PhysicalClient.Tariff = intervalTariff;
			client.Update();
			Assert.That(client.GetPrice(), Is.EqualTo(100));
			SystemTime.Now = () => DateTime.Now.AddMonths(2).AddHours(1);
			Assert.That(client.GetPrice(), Is.EqualTo(200));
			var simpleTariff = new Tariff {
				Price = 300,
				Name = "simpleTariff",
				Description = "simpleTariff"
			};
			client.PhysicalClient.Tariff = simpleTariff;
			client.Update();
			Assert.That(client.GetPrice(), Is.EqualTo(300));
		}

		[Test]
		public void UserWriteOffTest()
		{
			PrepareTest();
			SystemTime.Reset();
			var client = CreateClient();
			client.Save();
			var lawyerPerson = new LawyerPerson {
				Balance = 1000
			};
			lawyerPerson.Save();
			client.PhysicalClient = null;
			client.LawyerPerson = lawyerPerson;
			client.UpdateAndFlush();
			new UserWriteOff {
				Client = client,
				Sum = 500,
				Date = SystemTime.Now(),
				Comment = string.Empty
			}.Save();
			billing.On();
			client.Refresh();
			Assert.That(client.LawyerPerson.Balance, Is.EqualTo(500m));
		}

		[Test]
		public void TimeTest()
		{
			var client = CreateClient();
			client.RatedPeriodDate = DateTime.Now;
			client.Update();
			var time = InternetSettings.FindFirst();
			time.NextBillingDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day - 1, 22, 00, 00);
			time.Update();
			SystemTime.Reset();
			billing.Run();
			Assert.That(WriteOff.Queryable.Count(w => w.Client == client), Is.EqualTo(1));
			billing.Run();
			billing.Run();
			Assert.That(WriteOff.Queryable.Count(w => w.Client == client), Is.EqualTo(1));
			SystemTime.Now = () => new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 22, 10, 00);
			billing.Run();
			Assert.That(WriteOff.Queryable.Count(w => w.Client == client), Is.EqualTo(2));
		}

		[Test]
		public void SemaphoreTest()
		{
			Client.DeleteAll();
			var client = CreateClient();
			client.PhysicalClient.ConnectionPaid = true;
			client.RatedPeriodDate = SystemTime.Now();
			client.Update();
			billing.Compute();
			var writeOff = WriteOff.Queryable.Where(w => w.Client == client).FirstOrDefault();
			Assert.That(writeOff, !Is.Null);
			writeOff.Delete();
			new Payment {
				Client = client,
				Sum = 100,
				BillingAccount = false
			}.Save();
			var set = InternetSettings.FindFirst();
			var dtn = SystemTime.Now();
			set.NextBillingDate = new DateTime(dtn.Year, dtn.Month, dtn.Day, 22, 00, 00);
			set.Save();
			SystemTime.Now = () => new DateTime(dtn.Year, dtn.Month, dtn.Day, 22, 20, 0);
			scope.Commit();
			scope.Dispose();
			scope = null;
			var ishBalance = 0m;
			using (new SessionScope()) {
				writeOff = WriteOff.Queryable.Where(w => w.Client == client).FirstOrDefault();
				client.Refresh();
				ishBalance = client.PhysicalClient.Balance;
				Assert.That(writeOff, Is.Null);
			}
			new Thread(() => billing.On()).Start();
			new Thread(() => billing.Run()).Start();
			Thread.Sleep(5000);
			using (new SessionScope()) {
				writeOff = WriteOff.Queryable.Where(w => w.Client == client).FirstOrDefault();
				client.Refresh();
				Assert.That(writeOff, !Is.Null);
				Assert.That(client.PhysicalClient.Balance, Is.EqualTo(Math.Round(ishBalance + 100 - client.GetPrice()/client.GetInterval(), 2)));
			}
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
			;
			billing.Compute();
			var spisD0 = WriteOff.Queryable.FirstOrDefault(w => w.Client == client);
			client.Refresh();
			Assert.That(dayInMonth, Is.EqualTo(client.GetInterval()));
			client.DebtDays = 29;
			client.Update();
			billing.Compute();
			var slisD29 = WriteOff.Queryable.Where(w => w.Client == client).ToList().LastOrDefault();
			client.Refresh();
			Assert.That(dayInMonth + 29, Is.EqualTo(client.GetInterval()));
		}

		[Test]
		public void Test1151()
		{
			var client = CreateClient();
			client.Disabled = false;
			client.RatedPeriodDate = new DateTime(2011, 5, 31, 15, 05, 23);
			SystemTime.Now = () => new DateTime(2011, 6, 30, 22, 02, 03);
			billing.Compute();
			client.Refresh();
			Assert.That(client.DebtDays, Is.EqualTo(1));
			SystemTime.Now = () => new DateTime(2011, 7, 31, 19, 03, 6);
			billing.Compute();
			client.Refresh();
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
			Assert.That(((DateTime) client.RatedPeriodDate).Date, Is.EqualTo(new DateTime(2011, 6, 15)));
		}

		[Test]
		public void DomolinkTariffTest()
		{
			var domTariff = new Tariff {
				Name = "domolink",
				Price = 0,
				PackageId = 8,
				Hidden = true,
				FinalPriceInterval = 1,
				FinalPrice = 590,
				Description = "domolink"
			};
			domTariff.Save();

			var physDom = new PhysicalClients {
				Name = "Александр",
				Surname = "Барабановский",
				Patronymic = "Тарасович",
				City = "Борисоглебск",
				Street = "Северный мкр.",
				Balance = 0m,
				Tariff = domTariff
			};
			physDom.Save();
			var domolinkClient = new Client {
				PhysicalClient = physDom,
				Disabled = true,
				Type = ClientType.Phisical,
				Name = "Александр Барабановский",
				DebtDays = 0,
				ShowBalanceWarningPage = false,
				RegDate = DateTime.Now,
				AutoUnblocked = false,
				PercentBalance = 0m
			};
			domolinkClient.Save();
			new Payment {
				Client = domolinkClient,
				Sum = 5m
			}.Save();

			billing.OnMethod();
			Assert.IsTrue(domolinkClient.AutoUnblocked);
			Assert.IsFalse(domolinkClient.Disabled);
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
			while (client.DebtDays < 1 && count < 365) {
				SystemTime.Now = () => new DateTime(2011, 6, 7, 22, 15, 9).AddDays(count);
				billing.Compute();
				client.Refresh();
				count++;
			}
			Assert.That(365, Is.EqualTo(count));
		}

		[Test]
		public void ShowBalWarning()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("ShowClient", false, 300);
			client.BeginWork = DateTime.Now;
			client.RatedPeriodDate = DateTime.Now;
			client.Save();
			var partBalance = client.GetPrice()/client.GetInterval();
			client.PhysicalClient.Balance = partBalance*2 - 1;
			client.Update();
			billing.Compute();
			client.Refresh();
			Assert.IsTrue(client.ShowBalanceWarningPage);
			Assert.Greater(client.PhysicalClient.Balance, 0);
			new Payment {
				Client = client,
				Sum = 100,
				BillingAccount = false,
			}.Save();
			billing.OnMethod();
			client.Refresh();
			Assert.IsTrue(client.ShowBalanceWarningPage);
			new Payment {
				Client = client,
				Sum = client.GetPriceForTariff() - client.PhysicalClient.Balance,
				BillingAccount = false,
			}.Save();
			billing.OnMethod();
			client.Refresh();
			Assert.IsFalse(client.ShowBalanceWarningPage);
		}

		[Test]
		public void OnTest()
		{
			var unblockedClient = CreateClient();
			unblockedClient.AutoUnblocked = false;
			unblockedClient.Disabled = true;
			unblockedClient.PercentBalance = 0.8m;
			unblockedClient.Status = Status.Find((uint) StatusType.BlockedAndConnected);
			unblockedClient.Update();
			var phisClient = unblockedClient.PhysicalClient;
			billing.OnMethod();
			unblockedClient.Refresh();
			Assert.IsTrue(unblockedClient.Disabled);
			new Payment {
				Client = unblockedClient,
				Sum = 0
			}.Save();
			new Payment {
				Client = unblockedClient,
				Sum = unblockedClient.GetPriceForTariff()/2
			}.Save();
			billing.OnMethod();
			unblockedClient.Refresh();
			Assert.IsTrue(unblockedClient.Disabled);
			Assert.That(unblockedClient.PhysicalClient.Balance, Is.GreaterThan(0));
			phisClient.Tariff.FinalPrice = phisClient.Tariff.Price + 1000;
			phisClient.Tariff.FinalPriceInterval = 3;
			phisClient.Update();
			new Payment {
				Client = unblockedClient,
				Sum = unblockedClient.GetPriceForTariff()
			}.Save();
			billing.OnMethod();
			unblockedClient.Refresh();
			Assert.IsFalse(unblockedClient.Disabled);

			phisClient.Balance = -5;
			phisClient.Update();
			unblockedClient.Disabled = true;
			unblockedClient.Update();
			new Payment {
				Client = unblockedClient,
				Sum = 20
			}.Save();
			billing.OnMethod();
			unblockedClient.Refresh();
			Assert.IsTrue(unblockedClient.Disabled);
			new Payment {
				Client = unblockedClient,
				Sum = unblockedClient.GetPriceForTariff() - phisClient.Balance
			}.Save();
			billing.OnMethod();
			unblockedClient.Refresh();
			Assert.IsFalse(unblockedClient.Disabled);
		}

		[Test]
		public void LawyerPersonTest()
		{
			PrepareTest();

			var lPerson = new LawyerPerson {
				Balance = 0,
				Tariff = 10000m,
			};
			lPerson.Save();
			var client = new Client {
				Disabled = false,
				Name = "TestLawyer",
				ShowBalanceWarningPage = false,
				LawyerPerson = lPerson
			};
			client.Save();

			for (int i = 0; i < DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)*2; i++) {
				billing.Compute();
			}
			Assert.That(-19999m, Is.GreaterThan(lPerson.Balance));
			Assert.That(-20000m, Is.LessThan(lPerson.Balance));
			billing.OnMethod();
			Assert.IsTrue(client.ShowBalanceWarningPage);
			lPerson.Balance += 1000;
			billing.OnMethod();
			Assert.IsTrue(!client.ShowBalanceWarningPage);
		}

		[Test]
		public void Write_off()
		{
			BaseBillingFixture.CreateAndSaveInternetSettings();
			PrepareTest();
			var client = CreateClient();

			client.PhysicalClient.Balance = Tariff.FindFirst().Price;
			client.Update();
			var interval = new Interval("15.01.2011", "15.02.2011");
			var dayCount = interval.GetInterval();
			interval.dtTo = DateTime.Parse("15.01.2011");
			for (var i = 0; i < dayCount; i++) {
				interval.dtTo = interval.dtTo.AddDays(1);
				SetClientDate(client, interval);
			}
			Assert.That(Math.Round(Convert.ToDecimal(client.PhysicalClient.Balance), 2), Is.LessThan(0.00));
		}

		[Test]
		public void RealTest()
		{
			SystemTime.Reset();
			using (var t = new TransactionScope(OnDispose.Rollback)) {
				InternetSettings.DeleteAll();
				t.VoteCommit();
				BaseBillingFixture.CreateAndSaveInternetSettings();
			}
			using (var t = new TransactionScope(OnDispose.Rollback)) {
				var b = new MainBilling();
				b.Run();
				t.VoteCommit();
			}
			var thisSettings = InternetSettings.FindFirst().NextBillingDate;
			Assert.That(thisSettings.ToShortDateString(), Is.EqualTo(DateTime.Now.ToShortDateString()));
		}

		[Test]
		public void Complex_tariff()
		{
			var tariff = new Tariff {
				Price = 200,
				FinalPriceInterval = 1,
				FinalPrice = 400,
				Name = "Complex_tariff",
				Description = "Complex_tariff"
			};
			var client = new Client {
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
			WriteOff.DeleteAll();
			Client.DeleteAll();
		}


		/// <summary>
		/// Следить за состоянием client.DebtDays;
		/// </summary>
		[Test]
		public void IntervalTest()
		{
			BaseBillingFixture.CreateAndSaveInternetSettings();
			foreach (var writeOff in WriteOff.FindAll()) {
				writeOff.Delete();
			}
			foreach (var clientse in Client.FindAll()) {
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

			foreach (var date in dates) {
				client.DebtDays = 0;
				client.Update();
				for (int i = 0; i < date.Count - 1; i++) {
					SetClientDate(client, date[i]);
					Assert.That(date[i + 1].GetInterval(), Is.EqualTo(client.GetInterval()));
				}
			}
		}
	}
}
