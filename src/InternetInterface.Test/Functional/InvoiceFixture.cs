using System;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class InvoiceFixture : global::Test.Support.Web.WatinFixture2
	{
		private Client client;

		[SetUp]
		public void Setup()
		{
			client = new Client {
				Status = Status.Find((uint)StatusType.Worked)
			};
			var lawyerPerson = new LawyerPerson {
				Name = "ООО Рога и Копыта",
				Tariff = 10000,
			};
			client.LawyerPerson = lawyerPerson;
			var writeOffSum = (lawyerPerson.Tariff/30).Value;
			var writeOff = new WriteOff(client, writeOffSum, DateTime.Today.AddMonths(-1));

			session.Save(client);
			session.Save(writeOff);
		}

		[Test]
		public void View_invoices()
		{
			Open("Map/SiteMap.rails");
			Click("Счета");
			AssertText("Счета");
		}

		[Test]
		public void Edit_invoice()
		{
			Open("Invoices/Index");
			Click("Редактировать");
			AssertText("Редактирование счета");
			Click("Сохранить");
			AssertText("Сохранено");
		}
	}
}