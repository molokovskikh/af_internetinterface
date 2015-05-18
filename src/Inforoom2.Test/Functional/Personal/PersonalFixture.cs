using System.Linq;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure.Helpers;
using Inforoom2.Test.Infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Personal
{
	public class PersonalFixture : BaseFixture
	{
		public Client Client;

		[SetUp]
		public void Setup()
		{
			Client = DbSession.Query<Client>().First(i => i.Comment == ClientCreateHelper.ClientMark.normalClient.GetDescription());
			LoginForClient(Client);
			Assert.IsTrue(browser.PageSource.Contains("Бонусные программы"));
		}
	}
}