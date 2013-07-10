using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Queries;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class SearchFilterFixture : IntegrationFixture
	{
		private RegionHouse _region;

		[SetUp]
		public void SetUp()
		{
			_region = new RegionHouse {
				Name = "Тестовый регион1"
			};
			session.Save(_region);
		}

		[Test]
		public void SearchWithRegionTest()
		{
			var client = ClientHelper.Client();
			session.Save(client);
			client.PhysicalClient.HouseObj = new House {
				Region = _region,
				Street = "street",
				Case = "1",
				Number = 1
			};
			session.Save(client.PhysicalClient.HouseObj);
			Flush();
			var filter = new SeachFilter {
				Region = _region.Id
			};
			var result = filter.Find(true);
			Assert.That(result.Count(r => r.client.Id == client.Id), Is.EqualTo(1));
		}

		[Test]
		public void SearchLegalPersonWithRegion()
		{
			var client = ClientHelper.CreateLaywerPerson();
			client.LawyerPerson.Region = _region;
			Save(client.LawyerPerson);
			Save(client);
			Flush();
			var filter = new SeachFilter {
				Region = _region.Id
			};
			var result = filter.Find(true);
			Assert.That(result.Count(r => r.client.Id == client.Id), Is.EqualTo(1));
		}
	}
}
