using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Tools.Calendar;
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
			session.Save(lPerson);
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
			session.Save(SaleSettings.Defaults());
		}

		[Test(Description = "Проверяет корректное списание при активации заказа")]
		public void WriteOffInActivateOrderTest()
		{
			order.EndDate = SystemTime.Now().AddDays(30);
			order.OrderServices.Add(new OrderService(order, 10, false));
			order.OrderServices.Add(new OrderService(order, 13, false));
			order.OrderServices.Add(new OrderService(order, 300, isPeriodic: true));
			session.Save(order);

			Compute();
			var writeOff = session.Query<WriteOff>().Where(w => w.Client == lawyerClient).ToArray();
			Assert.That(writeOff.Sum(w => w.WriteOffSum), Is.EqualTo(23));
		}

		[Test(Description = "Проверяет корректное списание периодических услуг в последний день месяца")]
		public void WriteOffMonthPeriodicTest()
		{
			SystemTime.Now = () => new DateTime(2012, 4, 30);
			order.BeginDate = SystemTime.Now().AddDays(-40);
			order.EndDate = SystemTime.Now().AddDays(20);
			order.OrderServices.Add(new OrderService(order, 100, isPeriodic: true));
			session.Save(order);

			Compute();
			var writeOff = session.Query<WriteOff>().Where(w => w.Client == lawyerClient).ToArray();
			Assert.That(writeOff.Sum(w => w.WriteOffSum), Is.EqualTo(100));
		}

		[Test(Description = "Проверяет, что не производится списание по периодическим услугам в дни кроме первого дня месяца")]
		public void WriteOffPeriodicOneTimeTest()
		{
			order.BeginDate = SystemTime.Now().AddDays(-40);
			order.EndDate = SystemTime.Now().AddDays(40);
			order.OrderServices.Add(new OrderService(order, 100, isPeriodic: true));
			session.Save(order);

			for (var i = 1; i <= 30; i++) {
				SystemTime.Now = () => new DateTime(2012, 4, i);
				Compute();
			}
			var writeOff = session.Query<WriteOff>().Where(w => w.Client == lawyerClient).ToArray();
			Assert.That(writeOff.Sum(w => w.WriteOffSum), Is.EqualTo(100));
		}

		[Test(Description = "Проверяем списание при активации и деактивации в одном месяце")]
		public void MoneyBackAfterDeactivateOrderTest()
		{
			order.BeginDate = new DateTime(2012, 4, 1);
			order.EndDate = new DateTime(2012, 4, 20);
			var service = new OrderService(order, 300, true);
			order.OrderServices.Add(service);
			session.Save(order);

			SystemTime.Now = () => new DateTime(2012, 4, 30);
			Compute();
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

			Compute();
			var writeOffs = session.Query<WriteOff>().Where(w => w.Client == lawyerClient).ToList();
			Assert.AreEqual(writeOffs.Count, 2);
		}

		private void Compute()
		{
			session.Flush();
			if (session.Transaction.IsActive)
				session.Transaction.Commit();
			session.Clear();
			billing.SafeProcessClientEndpointSwitcher();
			billing.ProcessWriteoffs();
			billing.SafeProcessClientEndpointSwitcher();
		}

		[Test]
		public void Activate_diactivate_test_allDays()
		{
			ActivateDiactivate(DateTime.Today);
		}

		[Test]
		public void Activate_diactivate_test_FirstDay()
		{
			ActivateDiactivate(new DateTime(2013, 07, 01));
		}

		private void ActivateDiactivate(DateTime begin)
		{
			var endpoint = new ClientEndpoint { Client = lawyerClient };
			lawyerClient.Endpoints.Add(endpoint);
			session.Save(endpoint);

			order.BeginDate = begin;
			order.EndDate = begin.AddMonths(1);
			order.EndPoint = endpoint;
			order.OrderServices.Add(new OrderService(order, 3000, isPeriodic: true));
			order.OrderServices.Add(new OrderService(order, 200, isPeriodic: false));

			session.Save(order);
			session.Save(lawyerClient);

			var end = begin.AddMonths(1).LastDayOfMonth();
			foreach (var current in begin.DaysTo(end)) {
				SystemTime.Now = () => current;
				Compute();
			}
			var writeOffs = session.Query<WriteOff>().Where(w => w.Client.Id == lawyerClient.Id).ToList();
			var daysInThisMonth = begin.DaysInMonth();
			var daysInNextNonth = end.DaysInMonth();
			var thisMonthSum = Math.Round((decimal)3000 / daysInThisMonth * (daysInThisMonth - begin.Day + 1), 2);
			var nextMonthSum = Math.Round((decimal)3000 / daysInNextNonth * order.EndDate.Value.Day, 2);
			var sum = Math.Round(200 + thisMonthSum + nextMonthSum, 2);
			Assert.AreEqual(sum, writeOffs.Sum(w => w.WriteOffSum));
			lawyerClient = session.Get<Client>(lawyerClient.Id);
			var appealsList = lawyerClient.Appeals
				.Where(ap => ap.Appeal.Contains("Деактивирован заказ"))
				.ToList();
			Assert.AreEqual(1, appealsList.Count);
			var endpointa = lawyerClient.Endpoints.FirstOrDefault();
			session.Refresh(endpointa);
      Assert.AreEqual(1, lawyerClient.Endpoints.Count(s => s.Disabled));
		}

		[Test(Description = "Проверяет, отображается ли пользователю страница с предупреждением, если баланс уходит в минус")]
		public void LawyerPersonTest()
		{
			var param = ConfigurationManager.AppSettings["LawyerPersonBalanceWarningRate"];
			var rate = decimal.Parse(param, CultureInfo.InvariantCulture);

			var region = session.Query<RegionHouse>().First(r => r.Name == "Воронеж");
			var lPerson = new LawyerPerson(region);
			session.Save(lPerson);
			lawyerClient = new Client() {
				Name = "TestLawyer",
				LawyerPerson = lPerson
			};

			lawyerClient.LawyerPerson.Balance += 5000;
			session.Save(lawyerClient);
			SystemTime.Now = () => new DateTime(2013, 4, 1);
			var order = new Order {
				Client = lawyerClient,
				BeginDate = SystemTime.Now().AddDays(-1),
				EndDate = SystemTime.Now().AddYears(1),
			};
			order.OrderServices.Add(new OrderService(order, 10000, isPeriodic: true));

			session.Save(order);
			FlushAndCommit();

			var sn = SystemTime.Now();
			var days = sn.DaysInMonth() + sn.AddMonths(1).DaysInMonth() * rate;
			var beginData = new DateTime(sn.Year, sn.Month, 1);
			for (int i = 0; i < days; i++) {
				SystemTime.Now = () => beginData.AddDays(i);
				billing.ProcessWriteoffs();
			}

			session.Clear();
			lPerson = session.Get<LawyerPerson>(lPerson.Id);
			Assert.That(lPerson.Balance, Is.EqualTo(-15000m), lPerson.Id.ToString());
			billing.ProcessPayments();
			lPerson.Balance += 15000;
			session.Update(lPerson);
			session.Flush();
			//3 Так как 2 не к этому клиенту
			Assert_statistic_appeal();

			session.Clear();
			lawyerClient = session.Get<Client>(lawyerClient.Id);
			Assert.IsTrue(lawyerClient.ShowBalanceWarningPage);
			Assert.IsTrue(lawyerClient.Status == null); //смотрим, а вдруг его вырубило уже по отрицательному балансу. Изначально у клиента(тестового) статуса нет, но он появится при блокировке
			billing.ProcessPayments();

			session.Clear();
			lawyerClient = session.Get<Client>(lawyerClient.Id);
			Assert.IsFalse(lawyerClient.ShowBalanceWarningPage);
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