using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NUnit.Framework;
using Test.Support.log4net;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class AgentFixture : MainBillingFixture
	{
		[Test]
		public void Query_test()
		{
			using (new SessionScope()) {
				var request = new Request {
					Registrator = InitializeContent.Partner,
					RegDate = DateTime.Now,
					PaidBonus = false,
					Client = client,
					ApplicantName = string.Empty,
					ApplicantPhoneNumber = string.Empty,
					ApplicantEmail = string.Empty,
					Street = string.Empty,
					Tariff = Tariff.FindFirst()
				};
				request.Save();
				client.BeginWork = DateTime.Now;
				client.Request = request;
				client.Save();
				var bonusesClients = Client.Queryable.Where(c =>
					c.Request != null &&
						!c.Request.PaidBonus &&
						c.Request.Registrator != null &&
						c.BeginWork != null)
					.ToList();
				Assert.That(bonusesClients.Count, Is.GreaterThan(0));
			}
		}

		[Test]
		public void Paid_bonus_for_agent()
		{
			Request request;
			using (new SessionScope()) {
				request = new Request {
					Registrator = InitializeContent.Partner,
					RegDate = DateTime.Now,
					PaidBonus = false,
					Client = client,
					ApplicantName = string.Empty,
					ApplicantPhoneNumber = string.Empty,
					ApplicantEmail = string.Empty,
					Street = string.Empty,
					Tariff = Tariff.FindFirst()
				};
				request.Save();
			}
			using (new SessionScope()) {
				client.BeginWork = DateTime.Now;
				client.Request = request;
				client.Save();
			}
			billing.Compute();
			billing.Compute();
			using (new SessionScope()) {
				request.Refresh();
				Assert.IsFalse(request.PaidBonus);
				var payment = new Payment {
					Client = client,
					Sum = client.GetPriceForTariff()
				};
				payment.Save();
				client.Payments.Add(payment);
			}
			billing.OnMethod();
			billing.Compute();
			billing.Compute();
			using (new SessionScope()) {
				request.Refresh();
				Assert.IsTrue(request.PaidBonus);
				var payments = PaymentsForAgent.Queryable.Where(p => p.Agent == InitializeContent.Partner).ToList();
				Assert.That(payments.Count, Is.EqualTo(1));
			}
		}
	}
}