using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.MonoRail.TestSupport;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;
using Test.Support;
using NUnit.Framework;


namespace InternetInterface.Test.Integration
{
	public class AgentFilterFixture : IntegrationFixture
	{
		[Test]
		public void VirtualTest()
		{
			if(InitializeContent.Partner.AccesedPartner == null) {
				InitializeContent.Partner.AccesedPartner = new List<string>();
				InitializeContent.Partner.AccesedPartner.Add("SSI");
			}
			var client = new Client(new PhysicalClient(), new List<Service>());
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

			var af = new AgentFilter();
			af.Virtual = VirtualType.bonus;
			var res = af.Find();
			Assert.That(res.Count(t => !t.Virtual), Is.EqualTo(0));

			af.Virtual = VirtualType.nobonus;
			res = af.Find();
			Assert.That(res.Count(t => t.Virtual), Is.EqualTo(0));

			af.Virtual = VirtualType.all;
			res = af.Find();
			Assert.That(res.Count(t => t.Virtual), Is.Not.EqualTo(0));
			Assert.That(res.Count(t => !t.Virtual), Is.Not.EqualTo(0));
		}
	}
}
