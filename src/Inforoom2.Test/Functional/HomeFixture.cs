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

		/// <summary>
		/// Проверка на определения города клиента по ip адресу.
		///  Сначала cookie по городу чистятся. Но после первой авторизации,
		///  вывода вопроса о принадлежности к городу быть не должно (авторизация 
		///  получает город по ip и сохраняет в cookie). 
		/// </summary>
		[Test]
		public void CheckRegionDefinitionByIp()
		{
			// получение IP клиента
			var currentIp = IPAddress.Parse("1772617729");
			var leasedIp = Lease.GetLeaseForIp(currentIp.ToString(), DbSession);
			var currentClient = leasedIp.Endpoint.Client;
			// чистка cookie
			SetCookie("userCity", "");
			// авторизация клиента по IP
			NetworkLoginForClient(currentClient);
			// обновление страницы
			Open("/");
			// проверка отсуствия на форме панели с выводом вопроса о принадлежности к городу
			AssertNoText("ВЫБЕРИТЕ ГОРОД");
			// получение текущего города клиента из cookie
			var cookie = GetCookie("userCity");
			// сравнение значения из cookie с привязанным к Ip адресом (городом)
			Assert.That(cookie, Is.EqualTo(currentClient.Address.House.Street.Region.Name), "Город клиента определен и хранится в cookie.");
		}
	}
}