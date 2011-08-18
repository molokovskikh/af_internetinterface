using System.Linq;
using Castle.ActiveRecord;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class LogginFixture
	{
		[Test]
		public void Log_update_info()
		{
			WatinFixture.ConfigTest();

			using (new SessionScope())
			{
				var physicalClient = PhysicalClients.Queryable.First();
				var client = Clients.Queryable.First();
				physicalClient.Tariff = Tariff.Queryable.First(t => t != physicalClient.Tariff);
				client.Status = Status.Queryable.First(s => s != client.Status);
				physicalClient.Save();
				client.Save();
			}
		}
	}
}