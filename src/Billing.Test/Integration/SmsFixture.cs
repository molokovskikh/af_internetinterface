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
			_client.PhysicalClient.Balance = _client.GetPrice() / _client.GetInterval() + 1;
			_client.SendSmsNotifocation = true;
			_client.Update();
		}

		[Test]
		public void BorderDateSmsTest()
		{
			SystemTime.Now = () => new DateTime(2012, 3, 31, 22, 3, 0);
			billing.Compute();
			using (new SessionScope()) {
				var sms = SmsMessage.Queryable.Where(m => m.Client == _client);
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
			billing.Compute();
			using (new SessionScope()) {
				Assert.That(SmsMessage.Queryable.Count(m => m.Client == _client), Is.EqualTo(1));
				new Payment {
					Client = _client,
					Sum = 100
				}.Save();
			}
			billing.OnMethod();
			using (new SessionScope())
				Assert.That(SmsMessage.Queryable.Count(m => m.Client == _client), Is.EqualTo(0));
		}

		[Test]
		public void Generated_sms_for_simple_Client()
		{
			SystemTime.Now = () => DateTime.Now.Date.AddHours(22).AddMinutes(1);
			billing.Compute();
			var message = billing.Messages.FirstOrDefault();
			Assert.IsNotNull(message);
			var messageText =
				string.Format("Ваш баланс 1,00 руб. Завтра доступ в сеть будет заблокирован.");
			Assert.That(message.Text, Is.StringContaining(messageText));
			Assert.IsNullOrEmpty(message.PhoneNumber);
			var contact = new Contact {
				Client = _client,
				Date = DateTime.Now,
				Text = "9507738447",
				Type = ContactType.SmsSending
			};
			_client.Contacts = new List<Contact> { contact };
			contact.Save();
			var dtnAd = DateTime.Now.AddDays(1);
			var ShouldBeSend = new DateTime(dtnAd.Year, dtnAd.Month, dtnAd.Day, 12, 00, 00);
			Assert.That(message.ShouldBeSend, Is.EqualTo(ShouldBeSend));
		}

		[Test]
		public void Real_Sms_Test()
		{
			var contact = new Contact {
				Client = _client,
				Date = DateTime.Now,
				Text = "9507738447",
				Type = ContactType.SmsSending
			};
			_client.Contacts = new List<Contact> { contact };
			contact.Save();
			billing.Compute();
			var message = billing.Messages.FirstOrDefault();
			if (message != null) {
				message.ShouldBeSend = null;
			}
			//SmsHelper.SendMessage(message);
		}

		[Test]
		public void DateSmsTest()
		{
			IEnumerable<SmsMessage> sms;
			SystemTime.Now = () => DateTime.Now.Date.AddHours(15);
			billing.Compute();
			using (new SessionScope()) {
				sms = SmsMessage.Queryable.Where(m => m.Client == _client);
				foreach (var smsMessage in sms) {
					Assert.That(smsMessage.ShouldBeSend, Is.EqualTo(DateTime.Now.Date.AddHours(12)));
				}
				SystemTime.Now = () => DateTime.Now.Date.AddHours(22).AddMinutes(1);
				SmsMessage.DeleteAll();
			}
			billing.Compute();
			using (new SessionScope()) {
				sms = SmsMessage.Queryable.Where(m => m.Client == _client);
				foreach (var smsMessage in sms) {
					Assert.That(smsMessage.ShouldBeSend, Is.EqualTo(DateTime.Now.Date.AddDays(1).AddHours(12)));
				}
			}
		}

		[Test]
		public void Two_day_send_sms()
		{
			var messages = new List<SmsMessage>();
			var sum = _client.GetPrice() / _client.GetInterval();
			_client.PhysicalClient.Balance = sum * 3 + 1;
			_client.PhysicalClient.Update();
			billing.Compute();
			messages.AddRange(billing.Messages);
			Assert.AreEqual(messages.Count, 0);
			billing.Compute();
			messages.AddRange(billing.Messages);
			Assert.AreEqual(messages.Count, 1);
			Assert.That(messages[0].Text, Is.StringContaining("Послезавтра"));
			billing.Compute();
			messages.AddRange(billing.Messages);
			Assert.AreEqual(messages.Count, 2);
			Assert.That(messages[1].Text, Is.StringContaining("Завтра"));
			billing.Compute();
			messages.AddRange(billing.Messages);
			Assert.AreEqual(messages.Count, 2);
		}
	}
}