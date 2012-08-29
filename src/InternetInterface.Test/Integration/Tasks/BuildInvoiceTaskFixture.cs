using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using InternetInterface.Background;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration.Tasks
{
	[TestFixture, Ignore("Чинить")]
	public class BuildInvoiceTaskFixture : IntegrationFixture
	{
		[Test]
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
	}
}