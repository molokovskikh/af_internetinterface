using System;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Queries;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

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
			Prepare(controller);
		}

		[Test(Description = "Проверяет корректное создание акта и счета при создании или редактировании заказа"), Ignore("Отключет функционал")]
		public void CreateActAndInvoiceTest()
		{
			var client = ClientHelper.CreateLaywerPerson(session);
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

			var client = ClientHelper.Client(session);
			client.Status = worked;
			var endpoint = new ClientEndpoint(client, 1, commutator);
			client.Endpoints.Add(endpoint);
			var house = new House("Студенческая", 12, new RegionHouse("Тестовый регион"));
			session.Save(client);
			session.Save(house.Region);
			session.Save(house);

			session.Flush();
			Request.Form["_client.Status.id"] = dissolved.Id.ToString();
			controller.EditInformation(client.Id, null, house.Id, AppealType.All, null, new ClientFilter());
			CheckValidationError(client.PhysicalClient);

			session.Flush();
			session.Clear();
			endpoint = session.Get<ClientEndpoint>(endpoint.Id);
			client = session.Get<Client>(client.Id);
			Assert.True(client.Disabled);
			Assert.False(client.AutoUnblocked);
			Assert.That(endpoint, Is.EqualTo(null));
			var message = client.Appeals.Reverse().FirstOrDefault(a => a.Appeal.Contains("Коммутатор"));
			Assert.AreEqual("Коммутатор Тестовый коммутатор порт 1", message.Appeal);
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
			var client = ClientHelper.Client(session);
			client.Recipient = recipient;
			var payment = new Payment(client, 333);
			session.Save(payment);
			var bankPayment = new BankPayment(client, DateTime.Now, 333) { Recipient = recipient, Payment = payment };
			session.Save(recipient);
			session.Save(client);
			session.Save(bankPayment);

			var paymentsController = new PaymentsController();
			Prepare(paymentsController);
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
			var client = ClientHelper.Client(session);
			var writeOff = new WriteOff(client, 300) { VirtualSum = 150, MoneySum = 150 };
			client.PhysicalClient.Balance = 0;
			session.Save(client);
			session.Save(writeOff);

			controller.DeleteWriteOff(writeOff.Id, false);

			Assert.IsTrue(sended);
			Assert.That(email.Subject, Is.EqualTo("Уведомление об удалении списания"));
			Assert.That(email.Body, Is.StringContaining("Отменено списание №"));
			Assert.That(email.Body, Is.StringContaining("Клиент: №"));
			Assert.That(email.Body, Is.StringContaining("Сумма:"));
			Assert.That(email.Body, Is.StringContaining("Оператор:"));
			Assert.AreEqual(client.Balance, 300);
			Assert.AreEqual(client.PhysicalClient.MoneyBalance, 150);
			Assert.AreEqual(client.PhysicalClient.VirtualBalance, 150);
			session.Refresh(client);
			Assert.That(client.Appeals.First().Appeal, Is.StringContaining("Удалено списание на сумму"));
		}

		[Test]
		public void Delete_write_off_lawyer_person_test()
		{
			var client = ClientHelper.CreateLaywerPerson(session);
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
			var request = new Request { ApplicantName = name, ApplicantPhoneNumber = "900-9090900", Street = "123", Tariff = tariff, ActionDate = DateTime.Now };
			session.Save(request);
			var filter = new RequestFilter { query = request.Id.ToString() };
			var result = filter.Find(session);
			Assert.Greater(result.Count, 0);
			Assert.AreEqual(result[0].ApplicantName, name);
		}

		[Test]
		public void Filter_request_by_number_type()
		{
			var tariff = new Tariff("test", 100);
			session.Save(tariff);
			var request = new Request { ApplicantName = Generator.Name(), ApplicantPhoneNumber = "900-9090900", Street = "123", House = 2, Tariff = tariff, ActionDate = DateTime.Now };
			session.Save(request);
			var request2 = new Request { ApplicantName = Generator.Name(), ApplicantPhoneNumber = "900-9090900", Street = "123", House = 1, Tariff = tariff, ActionDate = DateTime.Now };
			session.Save(request2);

			var filter = new RequestFilter { HouseNumberType = HouseNumberType.Odd };
			var result = filter.Find(session).Select(r => r.Id).ToArray();
			Assert.Contains(request2.Id, result);
			Assert.That(result, Is.Not.Contains(request.Id));
		}

		[Test(Description = "При получении статического IP, у клиента заполняется инфа, которую заполняет ДХЦП сервер, при подключении клиента.")]
		public void DHCP_Prepared_Fields_on_static_clients()
		{
			var client = ClientHelper.Client(session);
			client.RatedPeriodDate = null;
			session.Save(client);
			Assert.That(client.BeginWork, Is.Null);
			Assert.That(client.RatedPeriodDate, Is.Null);

			var zone = new Zone("dasdad");
			session.Save(zone);
			var sw = new NetworkSwitch("lal", zone);
			session.Save(sw);

			var ip = new StaticIp();
			ip.Ip = "172.28.11.2";
			ip.Mask = 255;

			var info = new ConnectInfo();
			info.Port = "22";
			info.Switch = sw.Id;

			controller.SaveSwitchForClient(client.Id, info, 0, new StaticIp[1] { ip }, 0, "100", new Order(), false, 0);
			session.Flush();
			Assert.That(client.BeginWork, Is.Not.Null);
			Assert.That(client.RatedPeriodDate, Is.Not.Null);
		}
	}
}