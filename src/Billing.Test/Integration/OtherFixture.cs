using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using InternetInterface.Models;
using InternetInterface.Services;
using NHibernate.Linq;
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

		[Test]
		public void CheckDebtWork()
		{
			scope = new SessionScope();
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
			var counter = 0;
			for (var j = 1000; j < 20000; j++) {
				Client client = null;
				client = session.Query<Client>().FirstOrDefault(i => i.Id == j);

				if (client == null)
					continue;
				if (!client.HaveService<DebtWork>())
					continue;
				var active = client.FindActiveService<DebtWork>() == null;
				Console.WriteLine(String.Format("{3}Клиент: {0}, Сервис активен:{1}, будет заблокирован: {2}", client.Id, active, client.CanBlock(), "<br/>"));
				counter++;
			}
			Console.WriteLine("Всего клиентов: " + counter);
		}

		private void Assert_warning_page(bool assert)
		{
			using (new SessionScope())
				ActiveRecordMediator.Save(client);

			billing.ProcessWriteoffs();

			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.AreEqual(client.ShowBalanceWarningPage, assert);
			}
		}

		[Test]
		public void String_Parsing()
		{
			var one = "1.5";
			var two = "2.5";
			var val1 = (decimal)float.Parse(one, CultureInfo.InvariantCulture);
			var val2 = decimal.Parse(two, CultureInfo.InvariantCulture);
			Assert.That(val1, Is.EqualTo(1.5));
			Assert.That(val2, Is.EqualTo(2.5));
		}
	}
}
