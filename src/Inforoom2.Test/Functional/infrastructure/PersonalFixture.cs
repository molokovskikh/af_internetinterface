using System.Linq;
using System.Net;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional
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