using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using InternetInterface.Models;
using NUnit.Framework;
using Test.Support.log4net;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class BonusFixture : MainBillingFixture
	{
		[Test]
		public void Base_request_bonus()
		{
			Request request;
			List<Payment> payment;
			Client friend_client;
			using (new SessionScope()) {
				var tariff = new Tariff("test", 100);
				ActiveRecordMediator.Save(tariff);
				friend_client = new Client();
				friend_client.Save();
				client.BeginWork = DateTime.Now;
				client.PercentBalance = 0.8m;
				client.Save();
				request = new Request {
					Client = client,
					FriendThisClient = friend_client,
					ApplicantName = "testRequest",
					ApplicantPhoneNumber = "8-900-900-90-90",
					Street = "testStreet",
					Tariff = tariff
				};
				ActiveRecordMediator.Save(request);
			}
			billing.Compute();
			using (new SessionScope()) {
				payment = Payment.Queryable.Where(p => p.Client == friend_client).ToList();
				Assert.That(payment.Count, Is.EqualTo(0));
				Assert.IsFalse(request.PaidFriendBonus);
				new Payment {
					Client = client,
					Sum = client.GetPriceForTariff()
				}.Save();
			}
			billing.On();
			billing.Compute();
			using (new SessionScope()) {
				ActiveRecordMediator.Refresh(request);
				Assert.IsTrue(request.PaidFriendBonus);
				payment = Payment.Queryable.Where(p => p.Client == friend_client).ToList();
				Assert.That(payment.Count, Is.EqualTo(1));
			}
			billing.Compute();
			billing.Compute();
			using (new SessionScope()) {
				payment = Payment.Queryable.Where(p => p.Client == friend_client).ToList();
				Assert.That(payment.Count, Is.EqualTo(1));
				Assert.That(payment[0].Sum, Is.EqualTo(250m));
				Assert.That(payment[0].Comment, Is.StringContaining("Подключи друга"));
				Assert.IsTrue(payment[0].Virtual);
			}
		}
	}
}
