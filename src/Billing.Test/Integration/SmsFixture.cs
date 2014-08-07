using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	public class SmsFixture : MainBillingFixture
	{
		[SetUp]
		public void PrepareClientAndBilling()
		{
			client.PhysicalClient.Balance = client.GetPrice() / client.GetInterval() + 1;
			client.SendSmsNotification = true;
			client.Update();
			var contact = new Contact(client, ContactType.SmsSending, "9505611241");
			ActiveRecordMediator.Save(contact);
			client.Contacts.Add(contact);
		}

		[Test]
		public void BorderDateSmsTest()
		{
			SystemTime.Now = () => new DateTime(2012, 3, 31, 22, 3, 0);
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				var sms = SmsMessage.Queryable.Where(m => m.Client == client);
				foreach (var smsMessage in sms) {
					Assert.That(smsMessage.ShouldBeSend, Is.EqualTo(new DateTime(2012, 4, 1, 12, 00, 00)));
					return;
				}
			}
			throw new Exception();
		}

		[Test]
		public void Payment_and_sms_Test()
		{
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				Assert.That(SmsMessage.Queryable.Count(m => m.Client == client), Is.EqualTo(1));
				new Payment {
					Client = client,
					Sum = 100
				}.Save();
			}
			billing.ProcessPayments();
			using (new SessionScope())
				Assert.That(SmsMessage.Queryable.Count(m => m.Client == client), Is.EqualTo(0));
		}

		[Test]
		public void Generated_sms_сlient()
		{
			SystemTime.Now = () => DateTime.Now.Date.AddHours(22).AddMinutes(1);
			billing.ProcessWriteoffs();

			var message = billing.Messages.FirstOrDefault();
			var messageText =
				string.Format("Ваш баланс 1,00р. {0:d} доступ в сеть будет заблокирован.", DateTime.Now.Date.AddDays(1));
			Assert.IsNotNull(message);
			Assert.That(message.Text, Is.StringContaining(messageText));
			Assert.AreEqual("+79505611241", message.PhoneNumber);
			Assert.AreEqual(DateTime.Today.AddDays(1).Add(new TimeSpan(12, 00, 00)), message.ShouldBeSend);
		}

		[Test]
		public void Real_Sms_Test()
		{
			var contact = new Contact(client, ContactType.SmsSending, "9507738447");
			client.Contacts = new List<Contact> { contact };
			ActiveRecordMediator.Save(contact);
			billing.ProcessWriteoffs();
			var message = billing.Messages.FirstOrDefault();
			if (message != null) {
				message.ShouldBeSend = null;
			}
		}

		[Test]
		public void DateSmsTest()
		{
			IEnumerable<SmsMessage> sms;
			SystemTime.Now = () => DateTime.Now.Date.AddHours(15);
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				sms = SmsMessage.Queryable.Where(m => m.Client == client);
				foreach (var smsMessage in sms) {
					Assert.That(smsMessage.ShouldBeSend, Is.EqualTo(DateTime.Now.Date.AddHours(12)));
				}
				SystemTime.Now = () => DateTime.Now.Date.AddHours(22).AddMinutes(1);
				SmsMessage.DeleteAll();
			}
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				sms = SmsMessage.Queryable.Where(m => m.Client == client);
				foreach (var smsMessage in sms) {
					Assert.That(smsMessage.ShouldBeSend, Is.EqualTo(DateTime.Now.Date.AddDays(1).AddHours(12)));
				}
			}
		}

		[Test]
		public void Two_day_send_sms()
		{
			var messages = new List<SmsMessage>();
			var sum = client.GetPrice() / client.GetInterval();
			client.PhysicalClient.Balance = sum * 3 + 1;
			client.PhysicalClient.Update();
			billing.ProcessWriteoffs();
			messages.AddRange(billing.Messages);
			Assert.AreEqual(messages.Count, 0);
			billing.ProcessWriteoffs();
			messages.AddRange(billing.Messages);
			Assert.AreEqual(messages.Count, 1);
			Assert.That(messages[0].Text, Is.StringContaining(DateTime.Now.Date.AddDays(2).ToString("d")));
			billing.ProcessWriteoffs();
			messages.AddRange(billing.Messages);
			Assert.AreEqual(messages.Count, 2);
			Assert.That(messages[1].Text, Is.StringContaining(DateTime.Now.Date.AddDays(1).ToString("d")));
			billing.ProcessWriteoffs();
			messages.AddRange(billing.Messages);
			Assert.AreEqual(messages.Count, 2);
		}
	}
}