using System;
using System.Linq;
using InternetInterface.Models;
using NHibernate.Dialect.Function;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class BrigadFixture : IntegrationFixture
	{
		[Test]
		public void GetIntervals()
		{
			var brigad = new Brigad();
			session.Save(brigad);
			var client = new Client();
			session.Save(client);
			var connectGraph = new ConnectGraph(client, DateTime.Parse("2014-08-09 12:30:00"), brigad);
			session.Save(connectGraph);

			var intervals = brigad.GetIntervals(session, DateTime.Parse("2014-08-09"));

			Assert.AreEqual(connectGraph, intervals[7].Request);
			Assert.AreEqual(new TimeSpan(9, 0, 0), intervals.First().Begin);
			Assert.AreEqual(new TimeSpan(18, 0, 0), intervals.Last().End);
		}
	}
}