using System;
using System.Net;
using InternetInterface.Models;
using InternetInterface.Queries;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration.Queries
{
	[TestFixture]
	public class LeaseLogFilterFixture : IntegrationFixture
	{
		[Test]
		public void Find_leas()
		{
			var client = ClientHelper.Client(session);
			var zone = new Zone("Тестовая зона");
			var networkSwitch = new NetworkSwitch("Тестовый коммутатор", zone);
			client.Endpoints.Add(new ClientEndpoint(client, 1, networkSwitch));
			session.Save(zone);
			session.Save(networkSwitch);
			session.Save(client);

			var log = new Internetsessionslog {
				Id = client.Id,
				EndpointId = client.Endpoints[0],
				IP = "1541080067",
				LeaseBegin = DateTime.Now
			};
			session.Save(log);

			var filter = new LeaseLogFilter();
			filter.ClientCode = client.Id;
			var logs = filter.Find(session);
			Assert.AreEqual(1, logs.Count);
			Assert.AreEqual("91.219.4.3", logs[0].Lease.GetNormalIp());
		}
	}
}