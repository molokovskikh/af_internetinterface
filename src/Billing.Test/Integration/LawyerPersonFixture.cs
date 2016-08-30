using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class LawyerPersonFixture : MainBillingFixture
	{
		private Client lawyerClient;

		[SetUp]
		public void Up()
		{
			using (new SessionScope()) {
				var lPerson = new LawyerPerson {
					Balance = -3000,
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
				lPerson.Save();
				lawyerClient = new Client() {
					Disabled = false,
					Name = "TestLawyer",
					ShowBalanceWarningPage = false,
					LawyerPerson = lPerson
				};
				lawyerClient.Orders = new List<Order>();
				lawyerClient.Save();
			}
		}

		[Test]
		public void Three_hours_warning_interval()
		{
			using (new SessionScope()) {
				lawyerClient.Refresh();
				Assert.IsNull(lawyerClient.WhenShowWarning);
				Assert.IsFalse(lawyerClient.SendEmailNotification);
			}
			billing.ProcessPayments();
			Assert_statistic_appeal();
			using (new SessionScope()) {
				lawyerClient.Refresh();
				Assert.IsNotNull(lawyerClient.WhenShowWarning);
				Assert.IsTrue(lawyerClient.SendEmailNotification);
				lawyerClient.ShowBalanceWarningPage = false;
				lawyerClient.Update();
			}
			billing.ProcessPayments();
			using (new SessionScope()) {
				lawyerClient.Refresh();
				Assert.IsFalse(lawyerClient.ShowBalanceWarningPage);
				SystemTime.Now = () => DateTime.Now.AddHours(2).AddMinutes(45);
			}
			billing.ProcessPayments();
			using (new SessionScope()) {
				lawyerClient.Refresh();
				Assert.IsFalse(lawyerClient.ShowBalanceWarningPage);
				SystemTime.Now = () => DateTime.Now.AddHours(3);
			}
			billing.ProcessPayments();
			Assert_statistic_appeal();
			using (new SessionScope()) {
				lawyerClient.Refresh();
				Assert.IsTrue(lawyerClient.ShowBalanceWarningPage);
				Assert.IsTrue(lawyerClient.SendEmailNotification);
			}
		}

		[Test, Ignore("Потерял актуальность в связи с изменением тарификации для юр. лиц")]
		public void Make_last_write_off_round_for_tariff()
		{
			for (var i = 1; i <= 30; i++) {
				SystemTime.Now = () => new DateTime(2012, 4, i);
				billing.SafeProcessClientEndpointSwitcher();
				billing.ProcessWriteoffs();
			}
			using (new SessionScope()) {
				var writeOffs = WriteOff.Queryable.Where(w => w.Client == lawyerClient).ToList();
				Assert.That(writeOffs.Sum(w => w.WriteOffSum), Is.EqualTo(1000));
			}
		}

		[Test]
		public void Disable_write_Off()
		{
			using (new SessionScope()) {
				var order = new Order() { BeginDate = DateTime.Now, Client = lawyerClient, OrderServices = new List<OrderService>() };
				var service = new OrderService() { Cost = 100, Description = "testService", Order = order };
				order.OrderServices.Add(service);
				lawyerClient.Disabled = true;
				ActiveRecordMediator.Save(service);
				ActiveRecordMediator.Save(order);
				ActiveRecordMediator.Save(lawyerClient);
			}
			billing.SafeProcessClientEndpointSwitcher();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				var writeOffs = ActiveRecordLinq.AsQueryable<WriteOff>().Where(w => w.Client == lawyerClient).ToList();
				Assert.AreEqual(writeOffs.Count, 1);
			}
		}
	}
}