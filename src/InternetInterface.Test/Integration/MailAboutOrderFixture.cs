﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Web.Hosting;
using Castle.Core.Smtp;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Helpers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support;
using Rhino.Mocks;


namespace InternetInterface.Test.Integration
{
	[TestFixture]
	class MailAboutOrderFixture : ControllerFixture
	{
		private UserInfoController controller;
		private Client client;
		private Order order;
		private OrderService orderService;

		[SetUp]
		public void Setup()
		{
			controller = new UserInfoController();
			Prepare(controller);
			MessageOrderHelper.Sender = _sender;
			client = ClientHelper.CreateLaywerPerson(session);
			session.Save(client);
			order = new Order{Client = client};
			orderService = new OrderService {
				Cost = 100,
				Description = "Тестовая услуга"
			};
			order.OrderServices = new List<OrderService>();
			order.OrderServices.Add(orderService);
			session.Save(orderService);
			session.Save(order);
			orderService.Order = order;
			session.Update(orderService);
			controller.SaveSwitchForClient(client.Id, new ConnectInfo(), 0, new StaticIp[0], 0, "100", order, true, 0);
			session.Flush();
		}

		[Test(Description = "Проверяет, создалось ли обращение и письмо при добавлении заказа")]
		public void InsertOrderSender()
		{
			session.Refresh(client);
			Assert.That(client.Appeals.LastOrDefault().Appeal, Is.StringContaining("Зарегистрировано создание заказа для Юр.Лица"));
			Assert.That(client.Appeals.Count, Is.EqualTo(1));
			Assert.That(email.Body, Is.StringContaining("Зарегистрировано создание заказа для Юр.Лица"));
		}

		[Test(Description = "Проверяет, создалось ли письмо при изменении заказа")]
		public void EditOrderSender()
		{
			order = session.Load<Order>(order.Id);
			order.EndDate = new DateTime(2013, 4, 20);
			session.Update(order);
			controller.SaveSwitchForClient(client.Id, new ConnectInfo(), 0, new StaticIp[0], 0, "100", order, true, 0);
			session.Flush();
			Assert.That(email.Body, Is.StringContaining("Зарегистрировано внесение изменений заказа для Юр.Лица"));
		}

		[Test(Description = "Проверяет, создалось ли обращение и письмо при закрытии заказа")]
		public void CloseOrderSender()
		{
			controller.CloseOrder(order.Id, new DateTime(2013, 4, 20));
			Assert.That(email.Body, Is.StringContaining("Зарегистрировано закрытие заказа для Юр.Лица"));
			session.Flush();
			session.Refresh(client);
			Assert.That(client.Appeals.Where(p => p.Appeal == "Зарегистрировано закрытие заказа для Юр.Лица").ToList().Count, Is.EqualTo(1));
		}

		[Test(Description = "Проверяет, создалось ли обращение и письмо при создании услуги")]
		public void InsertServiceOrderSender()
		{
			orderService = new OrderService {
				Order = order,
				Cost = 200,
				Description = "Тестовая услуга2"
			};
			order.OrderServices.Add(orderService);
			session.Save(orderService);
			session.Update(order);
			Assert.That(email.Body, Is.StringContaining("Зарегистрировано внесение изменений заказа для Юр.Лица"));
			session.Flush();
			session.Refresh(client);
			Assert.That(client.Appeals.Where(p => p.Appeal == "Зарегистрировано внесение изменений заказа для Юр.Лица").ToList().Count, Is.EqualTo(1));
		}

		[Test(Description = "Проверяет, создалось ли обращение и письмо при удалении услуги")]
		public void DeleteServiceOrderSender()
		{
			order.OrderServices.Remove(orderService);
			session.Delete(orderService);
			session.Update(order);
			session.Flush();
			session.Refresh(client);
			Assert.That(email.Body, Is.StringContaining("Зарегистрировано внесение изменений заказа для Юр.Лица"));
			Assert.That(client.Appeals.Where(p => p.Appeal == "Зарегистрировано внесение изменений заказа для Юр.Лица").ToList().Count, Is.EqualTo(1));
		}
	}
}
