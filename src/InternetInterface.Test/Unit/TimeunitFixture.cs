using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using Common.Tools;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Services;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class TimeunitFixture
	{
		[Test]
		public void Build_time_table()
		{
			var partner = new Partner();
			var registrator = new Partner();
			var date = new DateTime(2013, 12, 16);
			var requests = new List<ServiceRequest> {
				new ServiceRequest(registrator, partner, new DateTime(2013, 12, 16, 10, 00, 00)),
				new ServiceRequest(registrator, partner, new DateTime(2013, 12, 16, 17, 00, 00))
			};
			var table = Timeunit.FromRequests(date, partner, requests);
			Assert.AreEqual(20, table.Count, table.Implode());
			Assert.AreEqual(new TimeSpan(9, 0, 0), table[0].Begin);
			Assert.AreEqual(new TimeSpan(9, 30, 0), table[0].End);
			Assert.AreEqual(new TimeSpan(18, 30, 0), table[19].Begin);

			Assert.AreEqual(2, table.Count(u => u.Busy));
			Assert.IsTrue(table[2].Busy, table[2].ToString());
			Assert.IsTrue(table[16].Busy, table[15].ToString());
			Assert.AreEqual(requests[0], table[2].Request);
		}
	}
}