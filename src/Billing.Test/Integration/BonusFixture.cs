using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
			var friend_client = new Client();
			friend_client.Save();
			_client.BeginWork = DateTime.Now;
			_client.Update();
			var request = new Request {
				Client = _client,
				FriendThisClient = friend_client
			};
			request.Save();
			_client.Refresh();
			billing.Compute();
			var payment = Payment.Queryable.Where(p => p.Client == friend_client).ToList();
			Assert.That(payment.Count, Is.EqualTo(0));
			Assert.IsFalse(request.PaidFriendBonus);
			new Payment {
				Client = _client,
				Sum = _client.GetPriceForTariff()
			}.Save();
			_client.Refresh();
			billing.Compute();
			Assert.IsTrue(request.PaidFriendBonus);
			payment = Payment.Queryable.Where(p => p.Client == friend_client).ToList();
			Assert.That(payment.Count, Is.EqualTo(1));
			billing.Compute();
			_client.Refresh();
			billing.Compute();
			_client.Refresh();
			payment = Payment.Queryable.Where(p => p.Client == friend_client).ToList();
			Assert.That(payment.Count, Is.EqualTo(1));
		}
	}
}
