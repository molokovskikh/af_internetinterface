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
			var endpoint = new ClientEndpoint() { IsEnabled = true, Client = client };
			ActiveRecordMediator.Save(endpoint);
			client.Contacts.Add(contact);
			client.Endpoints.Add(endpoint);
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
	}
}