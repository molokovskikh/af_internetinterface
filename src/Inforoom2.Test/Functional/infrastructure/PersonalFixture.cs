using System.Linq;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.infrastructure
{
	public class PersonalFixture : BaseFixture
	{
		public Client Client;

		[SetUp]
		public void Setup()
		{
			Client = DbSession.Query<Client>().First(i => i.PhysicalClient.Surname == "Кузнецов");
			LoginForClient(Client);
			Assert.IsTrue(browser.PageSource.Contains("Бонусные программы"));
		}
	}
}