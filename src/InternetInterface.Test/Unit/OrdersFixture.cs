using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class OrdersFixture
	{
		[TearDown]
		public void Teardown()
		{
			SystemTime.Reset();
		}

		[Test(Description = "Проверяет, что заказ остается редактируемым до 5-го числа след месяца")]
		public void Can_edit_test()
		{
			var order = new Order {
				BeginDate = DateTime.Now
			};
			Assert.IsTrue(order.CanEdit());
			SystemTime.Now = () => new DateTime(DateTime.Now.AddMonths(1).Year, DateTime.Now.AddMonths(1).Month, 5);
			Assert.IsTrue(order.CanEdit());
			SystemTime.Now = () => new DateTime(DateTime.Now.AddMonths(1).Year, DateTime.Now.AddMonths(1).Month, 6);
			Assert.IsFalse(order.CanEdit());
		}

		[Test]
		public void Can_edit_in_last_month_of_year()
		{
			var order = new Order {
				BeginDate = new DateTime(2013, 12, 05),
				EndDate = new DateTime(2014, 01, 10)
			};
			SystemTime.Now = () => new DateTime(2013, 12, 4);
			Assert.IsTrue(order.CanEdit());
			SystemTime.Now = () => new DateTime(2014, 01, 6);
			Assert.IsFalse(order.CanEdit());
		}
	}
}
