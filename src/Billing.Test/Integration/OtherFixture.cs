using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class OtherFixture : MainBillingFixture
	{
		[SetUp]
		public void SetUp()
		{
			Assert.IsFalse(client.ShowBalanceWarningPage);
		}

		[Test]
		public void Show_warning_if_client_no_passport_data()
		{
			client.BeginWork = DateTime.Now.AddDays(-8);
			client.PhysicalClient.PassportNumber = null;

			Assert_warning_page(true);
		}

		[Test]
		public void No_show_warning_if_no_passport_date_and_begin_work_5_days()
		{
			client.BeginWork = DateTime.Now.AddDays(-5);
			client.PhysicalClient.PassportNumber = null;

			Assert_warning_page(false);
		}

		[Test]
		public void Show_warning_if_balance_less_than_zero()
		{
			client.BeginWork = DateTime.Now.AddDays(-8);
			client.PhysicalClient.PassportNumber = null;
			client.PhysicalClient.Balance = -5;

			Assert_warning_page(true);
		}

		[Test]
		public void No_show_warning_if_begin_work_null()
		{
			client.BeginWork = null;
			client.PhysicalClient.PassportNumber = null;

			Assert_warning_page(false);
		}

		private void Assert_warning_page(bool assert)
		{
			using (new SessionScope())
				ActiveRecordMediator.Save(client);

			billing.Compute();

			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.AreEqual(client.ShowBalanceWarningPage, assert);
			}
		}
	}
}
