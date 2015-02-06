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

		public void LoginForClient(Client Client)
		{
			Open("Account/Login");
			Assert.That(browser.PageSource,Is.StringContaining("Вход в личный кабинет"));
			var name = browser.FindElementByCssSelector(".Account.Login input[name=username]");
			var password = browser.FindElementByCssSelector(".Account.Login input[name=password]");
			name.SendKeys(Client.Id.ToString());
			password.SendKeys("password");
			browser.FindElementByCssSelector(".Account.Login input[type=submit]").Click();
		}
	}
}