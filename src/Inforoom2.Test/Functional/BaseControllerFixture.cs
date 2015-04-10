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
		[Test]
		public void CheckRegionDefinitionByIP()
		{
			var currentIP = IPAddress.Parse("1772617729"); 
			var leasedIp = Lease.GetLeaseForIp(currentIP.ToString(), DbSession);
			var current_client = leasedIp.Endpoint.Client;
			SetCookie("userCity", "");
			NetworkLoginForClient(current_client);
			Open("/"); 
			AssertNoText("ВЫБЕРИТЕ ГОРОД");
			var cookie = GetCookie("userCity");
			Assert.That(cookie, Is.EqualTo(current_client.Address.House.Street.Region.Name), "У клиента в куки регион такой же как в адресе");
		}
	}
}