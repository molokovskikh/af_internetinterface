using System;
using System.Linq;
using System.Net;
using System.Web.UI.WebControls;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace Inforoom2.Test.Functional
{
	/// <summary>
	/// Проверка определения города
	/// </summary>
	[TestFixture]
	[Ignore]
	public class HomeIndexFixture : BaseFixture
	{
		protected Question Question;

		[Test, Description("Проверка определения города")]
		public void CitySelectTest()
		{
			Open();
			AssertText("ВЫБЕРИТЕ ГОРОД");
			var bt = browser.FindElement(By.XPath("//div[@class='buttons']//button[@class='button cancel']"));
			bt.Click();
			var link = browser.FindElement(By.XPath("//div[@class='cities']//a[text()='Борисоглебск']"));
			link.Click();
			var userCity = GetCookie("userCity");
			Assert.That(userCity, Is.EqualTo("Борисоглебск"));
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