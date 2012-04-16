using System.Linq;
using InternetInterface.Background;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration.Tasks
{
	[TestFixture]
	public class BuildInvoiceTaskFixture : IntegrationFixture
	{
		[Test]
		public void Build_invoices()
		{
			var client = new Client {
				Status = Status.Find((uint)StatusType.Worked)
			};
			var lawyerPerson = new LawyerPerson {
				Name = "ООО Рога и Копыта",
				Tariff = 10000,
			};
			client.LawyerPerson = lawyerPerson;
			var writeOffSum = (lawyerPerson.Tariff/30).Value;
			var writeOff = new WriteOff(client, writeOffSum);

			session.Save(client);
			session.Save(writeOff);

			var task = new BuildInvoiceTask();
			task.session = session;
			task.Process();

			var invoices = session.Query<Invoice>().Where(i => i.Client == client).ToList();
			Assert.That(invoices.Count, Is.EqualTo(1));
			var invoice = invoices[0];
			Assert.That(invoice.Sum, Is.EqualTo(writeOffSum));
			Assert.That(invoice.Parts.Count, Is.EqualTo(1));
			Assert.That(invoice.Parts[0].Sum, Is.EqualTo(writeOffSum));
		}
	}
}