using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class PhysicalClientFixture
	{
		[Test]
		public void Get_cut_address()
		{
			var client = new PhysicalClients {
				Street = "Студенческая",
				House = 12,
				CaseHouse = "а",
				Apartment = 100
			};
			Assert.That(client.GetCutAdress(), Is.EqualTo("ул. Студенческая д. 12 Корп а кв. 100"));
		}
	}
}