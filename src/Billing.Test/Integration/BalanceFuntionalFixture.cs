using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using InternetInterface.Services;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace Billing.Test.Integration
{
	public class BalanceFuntionalFixture : MainBillingFixture
	{
		[Test]
		public void Before_write_off_balance_test()
		{
			var oldBalance = _client.Balance;
			billing.Compute();
			ArHelper.WithSession(s => {
				_client = s.Get<Client>(_client.Id);
				var writeOff = _client.WriteOffs.First();
				Assert.That(writeOff.BeforeWriteOffBalance, Is.EqualTo(oldBalance));
			});
		}

		[Test]
		public void PastRatedPeriodDate()
		{
			using (new SessionScope()) {
				_client.RatedPeriodDate = DateTime.Now.AddMonths(-1);
				_client.Update();
			}
			billing.Compute();
			using (new SessionScope()) {
				_client.Refresh();
				Assert.That(_client.RatedPeriodDate.Value.Date, Is.EqualTo(DateTime.Now.Date));
				_client.RatedPeriodDate = DateTime.Now.AddMonths(-3).AddDays(-5);
				_client.Update();
			}
			billing.Compute();
			using (new SessionScope()) {
				_client.Refresh();
				Assert.That(_client.RatedPeriodDate.Value.Date, Is.EqualTo(DateTime.Now.Date));
				var rpd = new DateTime(2012, 1, 31, 22, 10, 11);
				_client.RatedPeriodDate = rpd;
				_client.StartNoBlock = rpd;
				SystemTime.Now = () => new DateTime(2012, 2, 29, 22, 10, 10);
				_client.Update();
			}
			billing.Compute();
			using (new SessionScope()) {
				_client.Refresh();
				Assert.That(_client.RatedPeriodDate.Value.Date, Is.EqualTo(new DateTime(2012, 2, 29)));
				Assert.That(_client.DebtDays, Is.EqualTo(2));
				SystemTime.Now = () => new DateTime(2012, 3, 30, 22, 10, 10);
			}
			billing.Compute();
			using (new SessionScope()) {
				_client.Refresh();
				Assert.That(_client.RatedPeriodDate.Value.Date, Is.EqualTo(new DateTime(2012, 2, 29)));
				Assert.That(_client.DebtDays, Is.EqualTo(2));
				SystemTime.Now = () => new DateTime(2012, 3, 31, 22, 10, 10);
			}
			billing.Compute();
			using (new SessionScope()) {
				_client.Refresh();
				Assert.That(_client.RatedPeriodDate.Value.Date, Is.EqualTo(new DateTime(2012, 3, 31)));
				Assert.That(_client.DebtDays, Is.EqualTo(0));
			}
		}

		[Test]
		public void TestBillingDate()
		{
			InternetSettings set;
			using (new SessionScope()) {
				set = InternetSettings.FindFirst();
				set.NextBillingDate = new DateTime(2011, 9, 30, 22, 00, 00);
				set.SaveAndFlush();
				SystemTime.Now = () => new DateTime(2011, 9, 30, 22, 10, 00);
			}
			billing.Run();
			using (new SessionScope()) {
				set.Refresh();
				Assert.That(set.NextBillingDate, Is.EqualTo(new DateTime(2011, 10, 1, 22, 00, 00)));
				SystemTime.Reset();
			}
		}

		[Test]
		public void TariffTest()
		{
			using (new SessionScope()) {
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
		}

		[Test]
		public void UserWriteOffTest()
		{
			Client client;
			using (new SessionScope()) {
				client = CreateClient();
				client.Save();
				var lawyerPerson = new LawyerPerson {
					Balance = 1000,
					Region = ArHelper.WithSession(s => {
				var region = s.Query<RegionHouse>().FirstOrDefault(r => r.Name == "Воронеж");
				if (region == null) {
					region = new RegionHouse {
						Name = "Воронеж"
					};
					s.Save(region);
				}
				return region;
			})
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
			}
			billing.OnMethod();
			using (new SessionScope()) {
				client.Refresh();
				Assert.That(client.LawyerPerson.Balance, Is.EqualTo(500m));
			}
		}

		[Test]
		public void TimeTest()
		{
			Client client;
			using (new SessionScope()) {
				client = CreateClient();
				client.RatedPeriodDate = DateTime.Now;
				client.Update();
				var time = InternetSettings.FindFirst();
				time.NextBillingDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day - 1, 22, 00, 00);
				time.Update();
				SystemTime.Reset();
			}
			billing.Run();
			using (new SessionScope())
				Assert.That(WriteOff.Queryable.Count(w => w.Client == client), Is.EqualTo(1));
			billing.Run();
			billing.Run();
			using (new SessionScope())
				Assert.That(WriteOff.Queryable.Count(w => w.Client == client), Is.EqualTo(1));
			SystemTime.Now = () => new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 22, 10, 00);
			billing.Run();
			using (new SessionScope())
				Assert.That(WriteOff.Queryable.Count(w => w.Client == client), Is.EqualTo(2));
		}

		[Test]
		public void SemaphoreTest()
		{
			Client client;
			WriteOff writeOff;
			using (new SessionScope()) {
				Client.DeleteAll();
				client = CreateClient();
				//client.PhysicalClient.ConnectionPaid = true;
				client.RatedPeriodDate = SystemTime.Now();
				client.Update();
			}
			billing.Compute();
			using (new SessionScope()) {
				writeOff = WriteOff.Queryable.FirstOrDefault(w => w.Client == client);
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
			}
			var ishBalance = 0m;
			using (new SessionScope()) {
				writeOff = WriteOff.Queryable.FirstOrDefault(w => w.Client == client);
				client.Refresh();
				ishBalance = client.PhysicalClient.Balance;
				Assert.That(writeOff, Is.Null);
			}
			var onTh = new Thread(() => billing.On());
			var runTh = new Thread(() => billing.Run());
			onTh.Start();
			runTh.Start();
			Thread.Sleep(5000);
			using (new SessionScope()) {
				writeOff = WriteOff.Queryable.FirstOrDefault(w => w.Client == client);
				client.Refresh();
				Assert.That(writeOff, !Is.Null);
				Assert.That(client.PhysicalClient.Balance, Is.EqualTo(Math.Round(ishBalance + 100 - client.GetPrice() / client.GetInterval(), 2)));
			}
			onTh.Join();
			onTh.Join();
		}

		[Test]
		public void MaxDebtTest()
		{
			var dayCount = SystemTime.Now().Day != 15 ? 15 : 14;
			var dayInMonth = DateTime.DaysInMonth(SystemTime.Now().AddDays(-dayCount).Year, SystemTime.Now().AddDays(-dayCount).Month);
			Client client;
			using (new SessionScope()) {
				client = CreateClient();
				client.Disabled = false;
				SystemTime.Reset();
				client.RatedPeriodDate = SystemTime.Now().AddDays(-dayCount);
				client.Update();
			}
			billing.Compute();
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.That(dayInMonth, Is.EqualTo(client.GetInterval()));
				client.DebtDays = 29;
				client.Update();
			}
			billing.Compute();
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.That(dayInMonth + 29, Is.EqualTo(client.GetInterval()));
			}
		}

		[Test]
		public void Test1151()
		{
			Client client;
			using (new SessionScope()) {
				client = CreateClient();
				client.Disabled = false;
				client.RatedPeriodDate = new DateTime(2011, 5, 31, 15, 05, 23);
				SystemTime.Now = () => new DateTime(2011, 6, 30, 22, 02, 03);
			}
			billing.Compute();
			using (new SessionScope()) {
				client.Refresh();
				Assert.That(client.DebtDays, Is.EqualTo(1));
				SystemTime.Now = () => new DateTime(2011, 7, 31, 19, 03, 6);
			}
			billing.Compute();
			using (new SessionScope()) {
				client.Refresh();
				Assert.That(client.DebtDays, Is.EqualTo(0));
			}
		}

		[Test]
		public void TetsDebtDays()
		{
			Client client;
			using (new SessionScope()) {
				client = CreateClient();
				client.Disabled = false;
				client.RatedPeriodDate = new DateTime(2011, 5, 15, 15, 05, 23);
				SystemTime.Now = () => new DateTime(2011, 6, 15, 22, 02, 03);
			}
			billing.Compute();
			using (new SessionScope()) {
				client.Refresh();
				Assert.That(client.DebtDays, Is.EqualTo(0));
				Assert.That(((DateTime)client.RatedPeriodDate).Date, Is.EqualTo(new DateTime(2011, 6, 15)));
			}
		}

		[Test]
		public void DomolinkTariffTest()
		{
			Client domolinkClient;
			using (new SessionScope()) {
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

				var physDom = new PhysicalClient {
					Name = "Александр",
					Surname = "Барабановский",
					Patronymic = "Тарасович",
					City = "Борисоглебск",
					Street = "Северный мкр.",
					Balance = 0m,
					Tariff = domTariff
				};
				domolinkClient = new Client(physDom, BaseBillingFixture.DefaultServices()) {
					Disabled = true,
					Type = ClientType.Phisical,
					Name = "Александр Барабановский",
				};
				domolinkClient.Save();
				new Payment {
					Client = domolinkClient,
					Sum = 5m
				}.Save();
			}
			billing.OnMethod();
			billing.OnMethod();
			using (new SessionScope()) {
				domolinkClient = Client.Find(domolinkClient.Id);
				Assert.IsTrue(domolinkClient.AutoUnblocked);
				Assert.IsFalse(domolinkClient.Disabled);
			}
			Assert_statistic_appeal();
		}

		[Test]
		public void FindDebt()
		{
			var count = 0;
			Client client;
			using (new SessionScope()) {
				client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 500000);
				client.Disabled = false;
				client.RatedPeriodDate = new DateTime(2011, 6, 9, 15, 00, 9);
				client.Save();
			}
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
			Client client;
			using (new SessionScope()) {
				client = BaseBillingFixture.CreateAndSaveClient("ShowClient", false, 300);
				client.BeginWork = DateTime.Now;
				client.RatedPeriodDate = DateTime.Now;
				client.PercentBalance = 0.8m;
				client.Save();
				var partBalance = client.GetPrice() / client.GetInterval();
				client.PhysicalClient.Balance = partBalance * 2 - 1;
				client.Update();
			}
			billing.Compute();
			Assert_statistic_appeal();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsTrue(client.ShowBalanceWarningPage);
				Assert.Greater(client.PhysicalClient.Balance, 0);
				new Payment {
					Client = client,
					Sum = 100,
					BillingAccount = false,
				}.Save();
			}
			billing.OnMethod();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsTrue(client.ShowBalanceWarningPage);
				new Payment {
					Client = client,
					Sum = client.GetPriceForTariff() - client.PhysicalClient.Balance,
					BillingAccount = false,
				}.Save();
			}
			billing.OnMethod();
			Assert_statistic_appeal();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsFalse(client.ShowBalanceWarningPage);
			}
		}

		[Test]
		public void OnTest()
		{
			Client unblockedClient;
			PhysicalClient phisClient;
			using (new SessionScope()) {
				unblockedClient = CreateClient();
				unblockedClient.AutoUnblocked = false;
				unblockedClient.Disabled = true;
				unblockedClient.PercentBalance = 0.8m;
				unblockedClient.Status = Status.Find((uint)StatusType.BlockedAndConnected);
				unblockedClient.Update();
				phisClient = unblockedClient.PhysicalClient;
			}
			billing.OnMethod();
			using (new SessionScope()) {
				unblockedClient.Refresh();
				Assert.IsTrue(unblockedClient.Disabled);
				new Payment {
					Client = unblockedClient,
					Sum = 0
				}.Save();
				new Payment {
					Client = unblockedClient,
					Sum = unblockedClient.GetPriceForTariff() / 2
				}.Save();
			}
			billing.OnMethod();
			using (new SessionScope()) {
				unblockedClient.Refresh();
				Assert.IsTrue(unblockedClient.Disabled);
				Assert.That(unblockedClient.PhysicalClient.Balance, Is.GreaterThan(0));
				phisClient = ActiveRecordMediator<PhysicalClient>.FindByPrimaryKey(phisClient.Id);
				phisClient.Tariff.FinalPrice = phisClient.Tariff.Price + 1000;
				phisClient.Tariff.FinalPriceInterval = 3;
				phisClient.Update();
				new Payment {
					Client = unblockedClient,
					Sum = unblockedClient.GetPriceForTariff()
				}.Save();
			}
			billing.OnMethod();
			Assert_statistic_appeal();
			using (new SessionScope()) {
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
			}
			billing.OnMethod();
			using (new SessionScope()) {
				unblockedClient.Refresh();
				Assert.IsTrue(unblockedClient.Disabled);
				new Payment {
					Client = unblockedClient,
					Sum = unblockedClient.GetPriceForTariff() - phisClient.Balance
				}.Save();
			}
			billing.OnMethod();
			Assert_statistic_appeal();
			using (new SessionScope()) {
				unblockedClient.Refresh();
				Assert.IsFalse(unblockedClient.Disabled);
			}
		}

		[Test]
		public void Write_off()
		{
			Client client;
			Interval interval;
			decimal dayCount;
			using (new SessionScope()) {
				client = CreateClient();

				client.PhysicalClient.Balance = client.PhysicalClient.Tariff.Price;
				client.Update();
				interval = new Interval("15.01.2011", "15.02.2011");
				dayCount = interval.GetInterval();
				interval.dtTo = DateTime.Parse("15.01.2011");
			}
			for (var i = 0; i < dayCount; i++) {
				interval.dtTo = interval.dtTo.AddDays(1);
				SetClientDate(interval, client);
			}
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.That(Math.Round(Convert.ToDecimal(client.PhysicalClient.Balance), 2), Is.LessThan(0.00));
			}
		}

		[Test]
		public void RealTest()
		{
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

			Client client;
			using (new SessionScope()) {
				client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 100);
				client.PhysicalClient.Tariff = tariff;
				client.BeginWork = DateTime.Now.AddMonths(-1);
				client.RatedPeriodDate = DateTime.Now;

				ActiveRecordMediator.Save(tariff);
				ActiveRecordMediator.Save(client);
			}

			billing.Compute();
			using (new SessionScope()) {
				var writeOffs = WriteOff.Queryable.Where(w => w.Client == client).ToList();
				Assert.That(writeOffs.Count, Is.EqualTo(1));
				Assert.That(writeOffs[0].WriteOffSum, Is.GreaterThan(10));
			}
		}

		/// <summary>
		/// Следить за состоянием client.DebtDays;
		/// </summary>
		[Test]
		public void IntervalTest()
		{
			Client client;
			using (new SessionScope())
				client = CreateClient();

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
				using (new SessionScope()) {
					client.DebtDays = 0;
					client.Update();
				}
				for (int i = 0; i < date.Count - 1; i++) {
					SetClientDate(date[i], client);
					using (new SessionScope()) {
						client.Refresh();
						Assert.That(date[i + 1].GetInterval(), Is.EqualTo(client.GetInterval()), date[i + 1].ToString());
					}
				}
			}
		}
	}
}