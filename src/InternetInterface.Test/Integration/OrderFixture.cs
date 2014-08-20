using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Castle.Components.Binder;
using Castle.Components.Validator;
using Common.Tools;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Services;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class OrderFixture : ControllerFixture
	{

		[Test]
		public void CreateOrder()
		{
			var runner = new ValidatorRunner(new CachedValidationRegistry());
			
			var serv = new OrderService();
			serv.Description = "Test case";
			Assert.True(runner.IsValid(serv));
			var descriptions = new string[] {
				"",
				"  ",
				"	",
				null
			} ;
			foreach(var d in descriptions) {
				serv.Description = d;
				Assert.False(runner.IsValid(serv));
			}
			serv.Description = "test";

			var order = new Order();
			var servs = new List<OrderService>();
			servs.Add(serv);
			order.OrderServices = servs;

			Assert.True(runner.IsValid(order));
			order.OrderServices = new List<OrderService>();
			Assert.False(runner.IsValid(order));

			order.OrderServices = new List<OrderService>();
			Assert.False(runner.IsValid(order));

		}

	}
}
