using System;
using System.Collections.Generic;
using System.Linq;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using Test.Support;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	public class AgentFilterFixture : IntegrationFixture
	{
		[Test]
		public void VirtualTest()
		{
			session.CreateSQLQuery("delete from internet.Payments;").ExecuteUpdate();
			if(InitializeContent.Partner.AccesedPartner == null) {
				InitializeContent.Partner.AccesedPartner = new List<string>();
				InitializeContent.Partner.AccesedPartner.Add("SSI");
			}
			var client = new Client(new PhysicalClient(), new Settings());
			session.Save(client);
			Payment payment = new Payment {
				Client = client,
				Sum = 100,
				Virtual = false,
				PaidOn = DateTime.Now
			};
			session.Save(payment);
			payment = new Payment {
				Client = client,
				Sum = 200,
				Virtual = true,
				PaidOn = DateTime.Now
			};
			session.Save(payment);

			var agentFilter = new AgentFilter();
			agentFilter.Virtual = VirtualType.Bonus;
			var filteredPayments = agentFilter.Find(session);
			Assert.That(filteredPayments.Count(t => !t.Virtual), Is.EqualTo(0));

			agentFilter.Virtual = VirtualType.NoBonus;
			filteredPayments = agentFilter.Find(session);
			Assert.That(filteredPayments.Count(t => t.Virtual), Is.EqualTo(0));

			agentFilter.Virtual = null;
			filteredPayments = agentFilter.Find(session);
			Assert.That(filteredPayments.Count(t => t.Virtual), Is.Not.EqualTo(0));
			Assert.That(filteredPayments.Count(t => !t.Virtual), Is.Not.EqualTo(0));
		}
	}
}
