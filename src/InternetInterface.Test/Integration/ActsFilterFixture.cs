using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class ActsFilterFixture : IntegrationFixture
	{
		[Test]
		public void SearchActsTest()
		{
			var client = ClientHelper.Client();
			session.Save(client);
			var act = new Act {
				Client = client,
				PayerName = "тестовый плательщик",
				Period = new Period(DateTime.Now)
			};
			session.Save(act);
			var filter = new ActsFilter {
				SearchText = "тестовый плательщик"
			};
			var result = filter.Find(session);
			Assert.That(result.Count, Is.GreaterThan(0));
			Assert.That(result.Any(r => r.Id == act.Id), Is.True);
		}
	}
}
