﻿using System;
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
			_client.PhysicalClient.Balance = _client.GetPrice()/_client.GetInterval() + 1;
			_client.SendSmsNotifocation = true;
			_client.Update();
		}

		[Test]
		public void Payment_and_sms_Test()
		{
			billing.Compute();
			Assert.That(SmsMessage.Queryable.Count(m => m.Client == _client), Is.EqualTo(1));
			new Payment {
				Client = _client,
				Sum = 100
			}.Save();
			billing.OnMethod();
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
			_client.Contacts = new List<Contact> {contact};
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
			_client.Contacts = new List<Contact> {contact};
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
			SystemTime.Now = () => DateTime.Now.Date.AddHours(15);
			billing.Compute();
			var sms = SmsMessage.Queryable.Where(m => m.Client == _client);
			foreach (var smsMessage in sms) {
				Assert.That(smsMessage.ShouldBeSend, Is.EqualTo(DateTime.Now.Date.AddHours(12)));
			}
			SystemTime.Now = () => DateTime.Now.Date.AddHours(22).AddMinutes(1);
			SmsMessage.DeleteAll();
			billing.Compute();
			sms = SmsMessage.Queryable.Where(m => m.Client == _client);
			foreach (var smsMessage in sms) {
				Assert.That(smsMessage.ShouldBeSend, Is.EqualTo(DateTime.Now.Date.AddDays(1).AddHours(12)));
			}
		}
	}
}
