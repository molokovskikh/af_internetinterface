using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class BillingOrdersFixture : IntegrationFixture
	{
		private Client lawyerClient;
		private Order order;

		[SetUp]
		public void SetUp()
		{
			CreateBilling();
			var region = session.Query<RegionHouse>().FirstOrDefault(r => r.Name == "Воронеж");
			if (region == null) {
				region = new RegionHouse {
					Name = "Воронеж"
				};
				session.Save(region);
			}
			var lPerson = new LawyerPerson {
				Balance = 2000,
				Region = region
			};
			var a = session.Save(lPerson);
			lawyerClient = new Client() {
				Disabled = false,
				Name = "TestLawyer",
				ShowBalanceWarningPage = false,
				LawyerPerson = lPerson
			};
			session.Save(lawyerClient);
			SystemTime.Now = () => new DateTime(2012, 4, 20);
			order = new Order {
				Client = lawyerClient,
				Number = 1
			};
			session.Save(order);
			session.Flush();
		}

		private MainBilling billing;
		private const int MaxSale = 15;
		private const int MinSale = 3;
		private const int PerionCount = 3;
		private const decimal SaleStep = 1m;

		public void CleanDb()
		{
			session.CreateSQLQuery(
					@"delete from Internet.ClientServices;
				delete from Internet.Requests;
				delete from Internet.SmsMessages;
				delete from Internet.UserWriteOffs;
				delete from Internet.WriteOff;
				delete from Internet.Payments;
				delete from Internet.Clients;
				delete from Internet.PhysicalClients;
				delete from Internet.PaymentsForAgent;
				delete from Internet.Appeals;
				delete from Internet.LawyerPerson;")
				.ExecuteUpdate();
		}

		public void CreateBilling()
		{
			SystemTime.Reset();
			billing = new MainBillingForTest();
			CleanDb();

			session.CreateSQLQuery("delete from Internet.SaleSettings").ExecuteUpdate();
			var saleSettings = new SaleSettings {
				MaxSale = MaxSale,
				MinSale = MinSale,
				PeriodCount = PerionCount,
				SaleStep = SaleStep
			};
			session.Save(saleSettings);
		}

		[Test(Description = "Проверяет корректное списание при активации заказа")]
		public void WriteOffInActivateOrderTest()
		{
			order.EndDate = SystemTime.Now().AddDays(30);
			var service = new OrderService {
				Cost = 10,
				Description = "1",
				IsPeriodic = false,
				Order = order
			};
			session.Save(service);
			service = new OrderService {
				Cost = 13,
				Description = "2",
				IsPeriodic = false,
				Order = order
			};
			session.Save(service);
			service = new OrderService {
				Cost = 300,
				Description = "3",
				IsPeriodic = true,
				Order = order
			};
			session.Save(service);
			Close();
			billing.Compute();
			var writeOff = session.Query<WriteOff>().Where(w => w.Client == lawyerClient).ToArray();
			Assert.That(writeOff.Sum(w => w.WriteOffSum), Is.EqualTo(133));
		}

		[Test(Description = "Проверяет корректное списание периодических услуг в первый день месяца")]
		public void WriteOffMonthPeriodicTest()
		{
			SystemTime.Now = () => new DateTime(2012, 4, 1);
			order.BeginDate = SystemTime.Now().AddDays(-20);
			order.EndDate = SystemTime.Now().AddDays(20);
			session.SaveOrUpdate(order);
			var service = new OrderService {
				Cost = 100,
				Description = "1",
				IsPeriodic = true,
				Order = order
			};
			session.Save(service);
			Close();
			billing.Compute();
			var writeOff = session.Query<WriteOff>().Where(w => w.Client == lawyerClient).ToArray();
			Assert.That(writeOff.Sum(w => w.WriteOffSum), Is.EqualTo(100));
		}

		[Test(Description = "Проверяет, что не производится списание по периодическим услугам в дни кроме первого дня месяца")]
		public void WriteOffPeriodicOneTimeTest()
		{
			order.BeginDate = SystemTime.Now().AddDays(-40);
			order.EndDate = SystemTime.Now().AddDays(40);
			session.SaveOrUpdate(order);
			var service = new OrderService {
				Cost = 100,
				Description = "1",
				IsPeriodic = true,
				Order = order
			};
			session.Save(service);
			Close();
			for (var i = 1; i <= 30; i++) {
				SystemTime.Now = () => new DateTime(2012, 4, i);
				billing.Compute();
			}
			var writeOff = session.Query<WriteOff>().Where(w => w.Client == lawyerClient).ToArray();
			Assert.That(writeOff.Sum(w => w.WriteOffSum), Is.EqualTo(100));
		}

		[Test(Description = "Проверяет корректность возврата денег за неиспользованные услуги при деактивации заказа")]
		public void MoneyBackAfterDeactivateOrderTest()
		{
			order.BeginDate = SystemTime.Now().AddDays(-40);
			session.SaveOrUpdate(order);
			var service = new OrderService {
				Cost = 300,
				Description = "1",
				IsPeriodic = true,
				Order = order
			};
			session.Save(service);
			session.Save(new WriteOff(lawyerClient, 300) { Service = service });
			Close();
			billing.Compute();
			var writeOff = session.Query<WriteOff>().Where(w => w.Client == lawyerClient && w.Service == service).ToArray();
			Assert.That(writeOff.Sum(w => w.WriteOffSum), Is.EqualTo(200));
		}

		[Test(Description = "Проверяет, что в один и тот же день можно и активировать и деактивировать заказы")]
		public void Activate_diactivate_one_day_test()
		{
			order.EndDate = SystemTime.Now().AddDays(30);
			var serviceForFirstOrder = new OrderService {
				Cost = 200,
				Order = order,
				IsPeriodic = true
			};
			session.Save(serviceForFirstOrder);
			session.Save(order);
			var order2 = new Order {
				Client = lawyerClient,
				BeginDate = order.BeginDate.Value.AddDays(-30),
				EndDate = order.BeginDate
			};
			session.Save(order2);
			var orderService = new OrderService {
				Order = order2,
				IsPeriodic = true,
				Cost = 300
			};
			session.Save(orderService);
			var writeOff = new WriteOff(lawyerClient, 300) { Service = orderService };
			session.Save(writeOff);
			Close();
			billing.Compute();
			var writeOffs = session.Query<WriteOff>().Where(w => w.Client == lawyerClient).ToList();
			Assert.AreEqual(writeOffs.Count, 2);
		}

		[Test]
		public void Activate_diactivate_test_allDays()
		{
			ActivateDiactivate(DateTime.Now);
		}

		[Test]
		public void Activate_diactivate_test_FirstDay()
		{
			ActivateDiactivate(new DateTime(2013, 07, 01));
		}

		private void ActivateDiactivate(DateTime now)
		{
			try {
				SystemTime.Reset();
				SystemTime.Now = () => now;
				var endpoint = new ClientEndpoint { Client = lawyerClient };
				lawyerClient.Endpoints.Add(endpoint);
				session.Save(endpoint);
				order.EndPoint = endpoint;
				var periodic = new OrderService {
					Cost = 3000,
					IsPeriodic = true,
					Order = order
				};
				session.Save(periodic);
				var noPeriodic = new OrderService {
					Cost = 200,
					Order = order
				};
				session.Save(noPeriodic);
				order.BeginDate = now;
				order.EndDate = now.AddMonths(1);
				order.OrderServices = new List<OrderService> { noPeriodic, periodic };
				session.Save(order);
				session.Save(lawyerClient);
				var dayCount = 0;
				Close();
				while (SystemTime.Now().Date <= now.AddMonths(1).Date) {
					billing.Compute();
					SystemTime.Now = () => now.AddDays(dayCount);
					dayCount++;
				}
				var writeOffs = session.Query<WriteOff>().Where(w => w.Client.Id == lawyerClient.Id).ToList();
				var daysInThisMonth = DateTime.DaysInMonth(now.Year, now.Month);
				var daysInNextNonth = DateTime.DaysInMonth(now.AddMonths(1).Year, now.AddMonths(1).Month);
				Assert.AreEqual(writeOffs.Sum(w => w.WriteOffSum), Math.Round(200 + (decimal)3000 / daysInThisMonth * (daysInThisMonth - now.Day + 1) + 3000 - (decimal)3000 / daysInNextNonth * (daysInNextNonth - now.AddMonths(1).Day), 2));
				lawyerClient = session.Get<Client>(lawyerClient.Id);
				Assert.That(lawyerClient.Appeals.First().Appeal, Is.StringContaining("Деактивирован заказ"));
				Assert.AreEqual(lawyerClient.Endpoints.Count, 0);
			}
			finally {
				SystemTime.Reset();
			}
		}

		[Test]
		public void LawyerPersonTest()
		{
			LawyerPerson lPerson;
			var region = session.Query<RegionHouse>().FirstOrDefault(r => r.Name == "Воронеж");
			if (region == null) {
				region = new RegionHouse {
					Name = "Воронеж"
				};
				session.Save(region);
			}
			lPerson = new LawyerPerson {
				Balance = 0,
				Region = region
			};
			session.Save(lPerson);

			lawyerClient = new Client() {
				Disabled = false,
				Name = "TestLawyer",
				ShowBalanceWarningPage = false,
				LawyerPerson = lPerson
			};
			session.Save(lawyerClient);
			SystemTime.Now = () => new DateTime(2013, 4, 1);
			var order = new Order {
				Client = lawyerClient,
				BeginDate = SystemTime.Now().AddDays(-1),
				EndDate = SystemTime.Now().AddYears(1),
				Number = 1,
			};
			session.Save(order);
			var orderSerive = new OrderService {
				Cost = 10000,
				IsPeriodic = true,
				Order = order
			};
			session.Save(orderSerive);
			Close();
			var sn = SystemTime.Now();
			var days = DateTime.DaysInMonth(sn.Year, sn.Month) + DateTime.DaysInMonth(sn.AddMonths(1).Year, sn.AddMonths(1).Month) + DateTime.DaysInMonth(sn.AddMonths(2).Year, sn.AddMonths(2).Month);
			var beginData = new DateTime(sn.Year, sn.Month, 1);
			for (int i = 0; i < days; i++) {
				SystemTime.Now = () => beginData.AddDays(i);
				billing.Compute();
			}
			session.Refresh(lPerson);
			Assert.That(-30000m, Is.EqualTo(lPerson.Balance));
			billing.OnMethod();
			lPerson.Balance += 1000;
			session.Update(lPerson);
			session.Flush();
			//3 Так как 2 не к этому клиенту
			Assert_statistic_appeal();
			session.Clear();
			session.Refresh(lawyerClient);
			Assert.IsTrue(lawyerClient.ShowBalanceWarningPage);

			billing.OnMethod();
			session.Refresh(lawyerClient);
			Assert.IsTrue(!lawyerClient.ShowBalanceWarningPage);
			Assert_statistic_appeal();
		}

		public void Assert_statistic_appeal(int appealCount = 1)
		{
			var appeals = session.Query<Appeals>().Where(a => a.AppealType == AppealType.Statistic).ToList();
			Assert.That(appeals.Count, Is.EqualTo(appealCount));
			session.CreateSQLQuery("delete from Internet.appeals").ExecuteUpdate();
		}
	}
}
