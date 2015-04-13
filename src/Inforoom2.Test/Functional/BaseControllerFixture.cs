using System.Linq;
using System.Net;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Billing;

namespace Inforoom2.Test.Functional
{
	/// <summary>
	/// </summary>
	public class BaseControllerFixture : BaseFixture
	{
		[Test]
		public void CheckNetworkLogin()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с низким балансом"));
			NetworkLoginForClient(client);
			Open("/");
			AssertText(client.Name);
			var cookie = GetCookie("networkClient");
			Assert.That(cookie, Is.EqualTo("true"), "У клиента нет куки залогиненого через сеть клиента");
		}
	}
}