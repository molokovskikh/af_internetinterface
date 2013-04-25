using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Common.Tools;
using InternetInterface.Background;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration.Tasks
{
	[TestFixture]
	public class BuildInvoiceTaskFixture : IntegrationFixture
	{
		[Test, Ignore("Чинить")]
		public void Build_invoices()
		{
			var messages = new List<MailMessage>();
			var mailer = Integration.MailerFixture.Prepare(m => messages.Add(m));

			var client = new Client() {
				Status = Status.Find((uint)StatusType.Worked)
			};
			var lawyerPerson = new LawyerPerson {
				Name = "ООО Рога и Копыта",
				Tariff = 10000,
			};
			client.LawyerPerson = lawyerPerson;
			var writeOffSum = (lawyerPerson.Tariff / 30).Value;
			var writeOff = new WriteOff(client, writeOffSum, DateTime.Today.AddMonths(-1));

			session.Save(client);
			session.Save(writeOff);

			var task = new BuildInvoiceTask(mailer);
			task.Process(session);

			var invoices = session.Query<Invoice>().Where(i => i.Client == client).ToList();
			Assert.That(invoices.Count, Is.EqualTo(1));
			var invoice = invoices[0];
			Assert.That(invoice.Sum, Is.EqualTo(writeOffSum));
			Assert.That(invoice.Parts.Count, Is.EqualTo(1));
			Assert.That(invoice.Parts[0].Sum, Is.EqualTo(writeOffSum));
			Assert.That(messages.Count, Is.GreaterThan(0));
		}

		private Client client;

		[SetUp]
		public void Setup()
		{
			client = ClientHelper.CreateLaywerPerson();
			client.LawyerPerson.Balance = -100000;
			session.Save(client);
		}

		[Test(Description = "Проверяем корректность создания счета")]
		public void BuildInvoiceTest()
		{
			var order = new Orders {
				BeginDate = SystemTime.Now().AddDays(-1),
				EndDate = SystemTime.Now().AddDays(100),
				Client = client,
				Number = 1
			};
			session.Save(order);
			var orderService = new OrderService {
				Order = order,
				Description = "Тестовая услуга1",
				Cost = 13,
				IsPeriodic = true
			};
			session.Save(orderService);
			order = new Orders {
				BeginDate = SystemTime.Now().AddDays(-1),
				EndDate = SystemTime.Now().AddDays(100),
				Client = client,
				Number = 12
			};
			session.Save(order);
			orderService = new OrderService {
				Order = order,
				Description = "Тестовая услуга2",
				Cost = 100,
				IsPeriodic = true
			};
			session.Save(orderService);
			session.Flush();
			Reopen();
			var task = new BuildInvoiceTask(null);
			task.MagicDate = task.MagicDate.AddMonths(-1);
			task.NotSend = true;
			task.Process(session);
			session.Flush();
			var invoice = session.Query<Invoice>().FirstOrDefault(i => i.Client == client);
			Assert.That(invoice.Sum, Is.EqualTo(113));
			Assert.That(invoice.Parts.Count, Is.EqualTo(2));
		}

		[Test(Description = "Проверяет корректность ежемесячного создания актов")]
		public void BuildActTest()
		{
			var order = new Orders {
				BeginDate = SystemTime.Now().AddDays(-100),
				EndDate = SystemTime.Now().AddDays(100),
				Client = client,
				Number = 1
			};
			session.Save(order);
			order.OrderServices = new List<OrderService>();
			var orderService = new OrderService {
				Order = order,
				Description = "Тестовая услуга1",
				Cost = 13,
				IsPeriodic = true
			};
			order.OrderServices.Add(orderService);
			session.Save(orderService);
			orderService = new OrderService {
				Order = order,
				Description = "Тестовая услуга2",
				Cost = 100,
				IsPeriodic = true
			};
			order.OrderServices.Add(orderService);
			session.Save(orderService);
			var invoice = new Invoice(client, DateTime.Today.AddMonths(-1).ToPeriod(), new Orders[] { order });
			invoice.Date = invoice.Date.AddMonths(-1);
			session.Save(invoice);
			Reopen();
			var task = new BuildInvoiceTask(null);
			task.MagicDate = task.MagicDate.AddMonths(-1);
			task.NotSend = true;
			task.Process(session);
			session.Flush();
			var act = session.Query<Act>().FirstOrDefault(a => a.Client == client);
			Assert.That(act.Sum, Is.EqualTo(113));
			Assert.That(act.Parts.Count, Is.EqualTo(2));
		}
	}
}