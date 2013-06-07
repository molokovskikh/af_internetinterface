﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.Core.Smtp;
using Castle.MonoRail.TestSupport;
using Common.Tools;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Queries;
using InternetInterface.Test.Helpers;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;
using Rhino.Mocks;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class UserInfoControllerFixture : ControllerFixture
	{
		private UserInfoController controller;

		[SetUp]
		public void Setup()
		{
			controller = new UserInfoController();
			PrepareController(controller);
		}

		[Test(Description = "Проверяет корректное создание акта и счета при создании или редактировании заказа"), Ignore("Отключет функционал")]
		public void CreateActAndInvoiceTest()
		{
			var client = ClientHelper.CreateLaywerPerson();
			session.Save(client);
			SystemTime.Now = () => new DateTime(2013, 4, 20);
			var order = new Order();
			var orderService = new OrderService {
				Cost = 50,
				Description = "Разовая",
				IsPeriodic = false
			};
			order.OrderServices = new List<OrderService> { orderService };
			orderService = new OrderService {
				Cost = 300,
				Description = "Периодичная",
				IsPeriodic = true
			};
			order.OrderServices.Add(orderService);
			orderService = new OrderService {
				Cost = 30,
				Description = "Периодичная",
				IsPeriodic = true
			};
			order.OrderServices.Add(orderService);

			controller.SaveSwitchForClient(client.Id, new ConnectInfo(), 0, new StaticIp[0], 0, "100", order, true, 0);
			session.Flush();
			var act = session.Query<Act>().FirstOrDefault(a => a.Client == client);
			Assert.That(act.Sum, Is.EqualTo(50));
			Assert.That(act.Parts[0].Name, Is.EqualTo("Разовая по заказу №" + order.Number));
			Assert.That(act.Parts[0].Cost, Is.EqualTo(50));
			var invoice = session.Query<Invoice>().FirstOrDefault(a => a.Client == client);
			Assert.That(invoice.Sum, Is.EqualTo(110));
			Assert.That(invoice.Parts[0].Name, Is.EqualTo("Периодичная по заказу №" + order.Number));
			Assert.That(invoice.Parts[0].Cost, Is.EqualTo(100));
			Assert.That(invoice.Parts[1].Name, Is.EqualTo("Периодичная по заказу №" + order.Number));
			Assert.That(invoice.Parts[1].Cost, Is.EqualTo(10));
		}

		[Test]
		public void Delete_all_endpoint_on_client_dissolve()
		{
			var statuses = session.Query<Status>().ToArray();
			var worked = statuses.First(s => s.Type == StatusType.Worked);
			var dissolved = statuses.First(s => s.Type == StatusType.Dissolved);
			var commutator = new NetworkSwitch("Тестовый коммутатор", session.Query<Zone>().First());
			session.Save(commutator);

			var client = ClientHelper.Client();
			client.Status = worked;
			var endpoint = new ClientEndpoint(client, 1, commutator);
			client.Endpoints.Add(endpoint);
			var house = new House("Студенческая", 12, new RegionHouse("Тестовый регион"));
			session.Save(client);
			session.Save(house.Region);
			session.Save(house);

			session.Flush();
			controller.EditInformation(client.Id, dissolved.Id, null, house.Id, AppealType.All, null, new ClientFilter());
			CheckValidationError(client.PhysicalClient);

			session.Flush();
			session.Clear();
			endpoint = session.Get<ClientEndpoint>(endpoint.Id);
			client = session.Get<Client>(client.Id);
			Assert.True(client.Disabled);
			Assert.False(client.AutoUnblocked);
			Assert.That(endpoint, Is.EqualTo(null));
			session.Clear();
		}

		private void CheckValidationError(object item)
		{
			var errors = controller.Validator.GetErrorSummary(item);
			if (errors != null && errors.ErrorsCount > 0)
				Assert.Fail(errors.ErrorMessages.Select((s, i) => Tuple.Create(errors.InvalidProperties[i], s)).Implode());
		}

		[Test]
		public void Delete_payment_test()
		{
			var recipient = new Recipient { Name = "testRecipient", BankAccountNumber = "40702810602000758601" };
			var client = ClientHelper.Client();
			client.Recipient = recipient;
			var payment = new Payment(client, 333);
			session.Save(payment);
			var bankPayment = new BankPayment(client, DateTime.Now, 333) { Recipient = recipient, Payment = payment };
			session.Save(recipient);
			session.Save(client);
			session.Save(bankPayment);

			var paymentsController = new PaymentsController();
			PrepareController(paymentsController);
			paymentsController.DbSession = session;
			paymentsController.Delete(bankPayment.Id);

			session.Flush();

			client = session.Get<Client>(client.Id);
			Assert.IsTrue(client.Appeals.Any(a => a.Appeal.Contains("Был удален банковский платеж от ")));
			Assert.IsFalse(client.Payments.Any(p => p.Sum == 333));
		}

		[Test]
		public void Delete_write_off_physical_client_test()
		{
			var client = ClientHelper.Client();
			var writeOff = new WriteOff(client, 300) { VirtualSum = 150, MoneySum = 150 };
			client.PhysicalClient.Balance = 0;
			session.Save(client);
			session.Save(writeOff);

			controller.DeleteWriteOff(writeOff.Id, false);

			Assert.IsTrue(sended);
			Assert.That(_message.Subject, Is.EqualTo("Уведомление об удалении списания"));
			Assert.That(_message.Body, Is.StringContaining("Отменено списание №"));
			Assert.That(_message.Body, Is.StringContaining("Клиент: №"));
			Assert.That(_message.Body, Is.StringContaining("Сумма:"));
			Assert.That(_message.Body, Is.StringContaining("Оператор:"));
			Assert.AreEqual(client.Balance, 300);
			Assert.AreEqual(client.PhysicalClient.MoneyBalance, 150);
			Assert.AreEqual(client.PhysicalClient.VirtualBalance, 150);
			session.Refresh(client);
			Assert.That(client.Appeals.First().Appeal, Is.StringContaining("Удалено списание на сумму"));
		}

		[Test]
		public void Delete_write_off_lawyer_person_test()
		{
			var client = ClientHelper.CreateLaywerPerson();
			client.LawyerPerson.Balance = 0;
			var writeOff = new WriteOff(client, 300);
			session.Save(client);
			session.Save(writeOff);

			controller.DeleteWriteOff(writeOff.Id, false);

			Assert.IsTrue(sended);
			Assert.AreEqual(client.Balance, 300);
			session.Refresh(client);
			Assert.That(client.Appeals.First().Appeal, Is.StringContaining("Удалено списание на сумму"));
		}

		[Test]
		public void Find_request_for_number()
		{
			var name = Generator.Name();
			var tariff = new Tariff("test", 100);
			session.Save(tariff);
			var request = new Request { ApplicantName = name, ApplicantPhoneNumber = "900-9090900", Street = "123", Tariff = tariff };
			session.Save(request);
			var filter = new RequestFilter { query = request.Id.ToString() };
			var result = filter.Find();
			Assert.AreEqual(result[0].ApplicantName, name);
		}
	}
}