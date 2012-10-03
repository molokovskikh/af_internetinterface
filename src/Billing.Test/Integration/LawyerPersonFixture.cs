﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using InternetInterface.Models;
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
					Balance = -2000,
					Tariff = 1000m,
				};
				lPerson.Save();
				lawyerClient = new Client() {
					Disabled = false,
					Name = "TestLawyer",
					ShowBalanceWarningPage = false,
					LawyerPerson = lPerson
				};
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
			billing.OnMethod();
			Assert_statistic_appeal();
			using (new SessionScope()) {
				lawyerClient.Refresh();
				Assert.IsNotNull(lawyerClient.WhenShowWarning);
				Assert.IsTrue(lawyerClient.SendEmailNotification);
				lawyerClient.ShowBalanceWarningPage = false;
				lawyerClient.Update();
			}
			billing.OnMethod();
			using (new SessionScope()) {
				lawyerClient.Refresh();
				Assert.IsFalse(lawyerClient.ShowBalanceWarningPage);
				SystemTime.Now = () => DateTime.Now.AddHours(2).AddMinutes(45);
			}
			billing.OnMethod();
			using (new SessionScope()) {
				lawyerClient.Refresh();
				Assert.IsFalse(lawyerClient.ShowBalanceWarningPage);
				SystemTime.Now = () => DateTime.Now.AddHours(3);
			}
			billing.OnMethod();
			Assert_statistic_appeal();
			using (new SessionScope()) {
				lawyerClient.Refresh();
				Assert.IsTrue(lawyerClient.ShowBalanceWarningPage);
				Assert.IsTrue(lawyerClient.SendEmailNotification);
			}
		}

		[Test]
		public void Make_last_write_off_round_for_tariff()
		{
			for (var i = 1; i <= 30; i++) {
				SystemTime.Now = () => new DateTime(2012, 4, i);
				billing.Compute();
			}
			using (new SessionScope()) {
				var writeOffs = WriteOff.Queryable.Where(w => w.Client == lawyerClient).ToList();
				Assert.That(writeOffs.Sum(w => w.WriteOffSum), Is.EqualTo(1000));
			}
		}

		[Test]
		public void Lawyer_person_payment_disable()
		{
			var balance = lawyerClient.Balance;
			using (new SessionScope()) {
				lawyerClient.Disabled = true;
				ActiveRecordMediator.Save(lawyerClient);
			}
			billing.OnMethod();
			using (new SessionScope()) {
				lawyerClient.Refresh();
				Assert.IsTrue(lawyerClient.Disabled);
				ActiveRecordMediator.Save(new Payment(lawyerClient, 100));
			}
			billing.OnMethod();
			using (new SessionScope()) {
				lawyerClient.Refresh();
				Assert.False(lawyerClient.Disabled);
				Assert.AreEqual(balance + 100, lawyerClient.Balance);
			}
		}

		[Test]
		public void Disable_write_Off()
		{
			using (new SessionScope()) {
				lawyerClient.Disabled = true;
				ActiveRecordMediator.Save(lawyerClient);
			}
			billing.Compute();
			using (new SessionScope()) {
				var writeOffs = ActiveRecordLinq.AsQueryable<WriteOff>().Where(w => w.Client == lawyerClient).ToList();
				Assert.AreEqual(writeOffs.Count, 1);
			}
		}
	}
}