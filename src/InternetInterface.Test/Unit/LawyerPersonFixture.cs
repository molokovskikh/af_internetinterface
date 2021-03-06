﻿using System;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using ExcelLibrary.BinaryFileFormat;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class LawyerPersonFixture
	{
		private LawyerPerson client;
		private Order order;

		[SetUp]
		public void Setup()
		{
			client = new LawyerPerson();
			var baseClient = new Client(client, new Partner(new UserRole()));
			client.client = baseClient;
			order = new Order(client) {
				BeginDate = new DateTime(2014, 2, 1)
			};
			client.client.Orders.Add(order);
		}

		[TearDown]
		public void Teardown()
		{
			SystemTime.Reset();
		}

		[Test]
		public void Calculate_write_off()
		{
			order.BeginDate = new DateTime(2014, 2, 1);
			order.OrderServices.Add(new OrderService(order, 500, isPeriodic: true));

			Assert.AreEqual(0, Sum(new DateTime(2014, 2, 10)));
			Assert.AreEqual(500, Sum(new DateTime(2014, 2, 28)));
		}

		[Test]
		public void Deactivate_order()
		{
			order.BeginDate = new DateTime(2014, 2, 1);
			order.EndDate = new DateTime(2014, 2, 10);
			order.OrderServices.Add(new OrderService(order, 500, isPeriodic: true));

			Assert.AreEqual(178.57, Sum(new DateTime(2014, 2, 10)));
			Assert.AreEqual(0, Sum(new DateTime(2014, 2, 11)));
			Assert.That(client.client.Appeals.Last().Appeal, Does.Contain("Деактивирован заказ"));
		}

		[Test, Ignore("Часть функционала была вынесена в биллинг, нужна сессия, подобная проверка есть в новой админке")]
		public void Write_off_non_periodic_services_on_activation()
		{
			order.BeginDate = new DateTime(2014, 2, 10);
			order.OrderServices.Add(new OrderService(order, 500, isPeriodic: true));
			order.OrderServices.Add(new OrderService(order, 1000, isPeriodic: false));

			Assert.AreEqual(0, Sum(new DateTime(2014, 2, 9)));
			Assert.AreEqual(1000, Sum(new DateTime(2014, 2, 10)));
			Assert.AreEqual(0, Sum(new DateTime(2014, 2, 11)));
		}

		[Test]
		public void Write_off_on_period_end()
		{
			order.BeginDate = new DateTime(2014, 2, 14);
			order.EndDate = new DateTime(2014, 2, 28);
			order.OrderServices.Add(new OrderService(order, 500, isPeriodic: true));

			Assert.AreEqual(267.86, Sum(new DateTime(2014, 2, 28)));
			Assert.AreEqual(0, Sum(new DateTime(2014, 3, 31)));
		}

		[Test]
		public void Recover_missed_calculation()
		{
			order.BeginDate = new DateTime(2014, 2, 14);
			order.OrderServices.Add(new OrderService(order, 500, isPeriodic: true));

			Assert.AreEqual(0, Sum(new DateTime(2014, 2, 14)));
			Assert.AreEqual(267.86, Sum(new DateTime(2014, 3, 2)));
		}

		[Test]
		public void Activeate_in_last_day_of_month()
		{
			order.BeginDate = new DateTime(2014, 2, 28);
			order.OrderServices.Add(new OrderService(order, 500, isPeriodic: true));

			Assert.AreEqual(17.86, Sum(new DateTime(2014, 2, 28)));
		}

		[Test, Ignore("Часть функционала была вынесена в биллинг, нужна сессия, подобная проверка есть в новой админке")]
		public void Calculate_tariff_on_active_orders()
		{
			order.EndDate = new DateTime(2014, 2, 28);
			order.OrderServices.Add(new OrderService(order, 500, isPeriodic: true));
			Sum(new DateTime(2014, 2, 27));
			Assert.AreEqual(500, client.Tariff);
			Sum(new DateTime(2014, 2, 28));
			Assert.AreEqual(0, client.Tariff);
		}

		[Test]
		public void Disable_order()
		{
			order.Disabled = true;
			order.OrderServices.Add(new OrderService(order, 600, isPeriodic: true));
			Assert.AreEqual(428.57, Sum(new DateTime(2014, 2, 20)));
			Assert.AreEqual(0, Sum(new DateTime(2014, 2, 28)));
		}

		[Test, Ignore("Часть функционала была вынесена в биллинг, нужна сессия, подобная проверка есть в новой админке")]
		public void Remove_unused_endpoint()
		{
			var endpoint = new ClientEndpoint(client.client, 1, new NetworkSwitch());
			client.client.Endpoints.Add(endpoint);
			order.EndPoint = endpoint;
			order.OrderServices.Add(new OrderService(order, 600, isPeriodic: true));

			Sum(new DateTime(2014, 2, 1));
			order.Disabled = true;
			Sum(new DateTime(2014, 2, 2));

			Assert.AreEqual(order.EndPoint.Disabled, true);
			Assert.AreEqual(0, client.client.Endpoints.Count(s => !s.Disabled));
			Assert.AreEqual(1, client.client.Endpoints.Count(s => s.Disabled));
		}

		[Test, Ignore("Часть функционала была вынесена в биллинг, нужна сессия, подобная проверка есть в новой админке")]
		public void Do_not_remove_endpoint_in_use()
		{
			var endpoint = new ClientEndpoint(client.client, 1, new NetworkSwitch());
			client.client.Endpoints.Add(endpoint);
			order.Client = client.client;
			order.EndPoint = endpoint;
			order.OrderServices.Add(new OrderService(order, 600, isPeriodic: true));
			var order1 = new Order {
				BeginDate = new DateTime(2014, 2, 1),
				EndPoint = endpoint
			};
			client.client.Orders.Add(order1);
			var listOfOrders = client.client.Orders.Where(o => !o.IsActivated && o.OrderStatus == OrderStatus.Enabled).ToArray();
			foreach (var item in listOfOrders) {
				item.Client = client.client;
			}
			Sum(new DateTime(2014, 2, 1));
			order.Disabled = true;
			Sum(new DateTime(2014, 2, 2));

			Assert.AreEqual(order.EndPoint.Disabled, true);
			Assert.AreEqual(0, client.client.Endpoints.Count(s => !s.Disabled));
			Assert.AreEqual(1, client.client.Endpoints.Count(s => s.Disabled));
		}

		private decimal Sum(DateTime dateTime)
		{
			SystemTime.Today = () => dateTime;
			SystemTime.Now = () => dateTime;
			return client.Calculate(dateTime).Sum(w => w.Sum);
		}
	}
}