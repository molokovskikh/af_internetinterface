using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Castle.ActiveRecord;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	public class FakeSmsHelper : SmsHelper
	{
		public XDocument Response { get; set; }
		public List<XDocument> Requests { get; set; }

		public FakeSmsHelper(XDocument response)
		{
			Response = response;
			Requests = new List<XDocument>();
		}

		protected override XDocument MakeRequest(XDocument document, string url)
		{
			Requests.Add(document);
			return Response;
		}
	}

	[TestFixture]
	public class SmsFixture : IntegrationFixture
	{
		private FakeSmsHelper Helper { get; set; }

		[SetUp]
		public void SetUp()
		{
			var document = new XDocument(
				new XElement("data",
					new XElement("code", "1"),
					new XElement("descr", "Операция успешно завершена"),
					new XElement("detail", null)));
			var dataElement = document.Element("data").Element("detail");
			var count = 0;
			foreach (var type in FakeSmsHelper.Types) {
				dataElement.Add(new XElement(type, new XElement("number", string.Format("7901000000{0}", count))));
				count++;
			}

			Helper = new FakeSmsHelper(document);
		}

		[Test]
		public void DeleteNoSendingTest()
		{
			var client = new Client();
			session.Save(client);
			session.Save(new SmsMessage(client, "473-2606000", "test_message"));

			Assert.That(session.Query<SmsMessage>().Count(m => m.Client == client), Is.GreaterThan(0));
			SmsHelper.DeleteNoSendingMessages(client);
			Assert.That(session.Query<SmsMessage>().Count(m => m.Client == client), Is.EqualTo(0));
		}

		[Test]
		public void Get_status_base_test()
		{
			var statuses = Helper.GetStatus(string.Empty);
			Assert.That(statuses.Count, Is.EqualTo(FakeSmsHelper.Types.Count));
			foreach (var statuse in statuses) {
				Assert.That(statuse.Value.Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void Get_numan_status()
		{
			Assert.That(Helper.GetStatus(new SmsMessage() { PhoneNumber = "79010000000" }), Is.EqualTo("Доставлено"));
			Assert.That(Helper.GetStatus(new SmsMessage() { PhoneNumber = "79010000001" }), Is.EqualTo("Не доставлено"));
			Assert.That(Helper.GetStatus(new SmsMessage() { PhoneNumber = "79010000002" }), Is.EqualTo("В ожидании"));
			Assert.That(Helper.GetStatus(new SmsMessage() { PhoneNumber = "79010000003" }), Is.EqualTo("Отчет о доставке еще не сформирован"));
			Assert.That(Helper.GetStatus(new SmsMessage() { PhoneNumber = "79010000004" }), Is.EqualTo("Отмена"));
			Assert.That(Helper.GetStatus(new SmsMessage() { PhoneNumber = "79010000005" }), Is.EqualTo("Сообщение находятся на модерации"));
		}

		[Test]
		public void Set_send_status()
		{
			var dada = "<data>"
				+ "<code>900</code>"
				+ "<descr>Временная ошибка</descr>"
				+ "</data>";
			Helper = new FakeSmsHelper(XDocument.Parse(dada));
			Helper.SaveSms = false;
			var sms = new SmsMessage(new Client(), "473-2606000", "test");
			Helper.SendMessage(sms);
			Assert.IsTrue(sms.IsFaulted);
		}
	}
}