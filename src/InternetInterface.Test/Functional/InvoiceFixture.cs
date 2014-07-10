using System;
using System.Linq;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	public class InvoiceFixture : SeleniumFixture
	{
		private Client client;

		[SetUp]
		public void Setup()
		{
			client = new Client() {
				Status = Status.Find((uint)StatusType.Worked)
			};
			var lawyerPerson = new LawyerPerson {
				Name = "ООО Рога и Копыта",
				Region = session.Query<RegionHouse>().First()
			};
			client.LawyerPerson = lawyerPerson;
			var writeOffSum = (lawyerPerson.Tariff / 30).Value;
			var writeOff = new WriteOff(client, writeOffSum, DateTime.Today.AddMonths(-1));

			var invoice = new Invoice(client, DateTime.Today.ToPeriod(), new[] { writeOff });
			session.SaveMany(client, writeOff, invoice);
		}

		[Test, Ignore("Отключет функционал")]
		public void View_invoices()
		{
			Open("Map/SiteMap");
			Click("Счета");
			AssertText("Счета");
		}

		[Test, Ignore("Отключет функционал")]
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
