using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
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
		public void Generated_sms_for_simple_Client()
		{
			billing.Compute();
			var message = billing.Messages.FirstOrDefault();
			Assert.IsNotNull(message);
			var messageText =
				string.Format(
					"Ваш баланс составляет 1,00 руб. При непоступлении оплаты, доступ в сеть будет заблокирован в течение суток.");
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
			billing.Compute();
			message = billing.Messages.FirstOrDefault();
			Assert.IsNotNull(message);
			messageText = string.Format("Ваш баланс составляет {0} руб. Доступ в интернет заблокирован.", (_client.PhysicalClient.Balance - _client.GetPrice()/_client.GetInterval()).ToString("0.00"));
			Assert.That(message.Text, Is.StringContaining(messageText));
			Assert.That(message.PhoneNumber, Is.StringContaining("9507738447"));
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
	}
}
