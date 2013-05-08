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
		[Test(Description = "Проверяет, что заказ остается редактируемым до 5-го числа след месяца")]
		public void Can_edit_test()
		{
			SystemTime.Reset();
			var order = new Order {
				BeginDate = DateTime.Now
			};
			Assert.IsTrue(order.CanEdit());
			var dn = DateTime.Now;
			SystemTime.Now = () => new DateTime(dn.AddMonths(1).Year, dn.AddMonths(1).Month, 5);
			Assert.IsTrue(order.CanEdit());
			SystemTime.Now = () => new DateTime(dn.AddMonths(1).Year, dn.AddMonths(1).Month, 6);
			Assert.IsFalse(order.CanEdit());
		}
	}
}
