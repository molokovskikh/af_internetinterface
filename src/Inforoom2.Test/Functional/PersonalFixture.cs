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
			Client = session.Query<Client>().First(i => i.PhysicalClient.Surname == "Кузнецов");
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

		[Test]
		public void FirstVisit()
		{
			Client = session.Query<Client>().First(i => i.PhysicalClient.Surname == "Третьяков");
			var internet = Client.ClientServices.First(i => (ServiceType) i.Service.Id == ServiceType.Internet);
			var iptv = Client.ClientServices.First(i => (ServiceType) i.Service.Id == ServiceType.Iptv);

			Assert.That(Client.Lunched,Is.False);
			Assert.That(Client.Endpoints.Count,Is.EqualTo(0));
			Assert.That(internet.IsActivated,Is.False);
			Assert.That(iptv.IsActivated,Is.False);
			LoginForClient(Client);
			AssertText("Серия паспорта");

			var series = browser.FindElementByCssSelector("input[name='physicalClient.PassportSeries']");
			series.SendKeys("1234");

			var number = browser.FindElementByCssSelector("input[name='physicalClient.PassportNumber']");
			number.SendKeys("123456");

			var date = browser.FindElementByCssSelector("input[name='physicalClient.PassportDate']");
			date.Click();
			var popup = browser.FindElementByCssSelector("a.ui-state-default");
			popup.Click();
			//date.SendKeys("18.12.2014");

			var residention = browser.FindElementByCssSelector("input[name='physicalClient.PassportResidention']");
			residention.SendKeys("Пасспортно-визовое отделение по району северный гор. Воронежа");

			var button = browser.FindElementByCssSelector(".right-block .button");
			button.Click();
			AssertText("успешно");
			session.Clear();
			var client= session.Query<Client>().First(i => i.PhysicalClient.Surname == "Третьяков");
			internet = client.ClientServices.First(i => (ServiceType) i.Service.Id == ServiceType.Internet);
			iptv = client.ClientServices.First(i => (ServiceType) i.Service.Id == ServiceType.Iptv);

			Assert.That(client.Lunched, Is.True);
			Assert.That(client.Endpoints.Count, Is.EqualTo(1));
			Assert.That(internet.IsActivated,Is.True);
			Assert.That(iptv.IsActivated,Is.True);
		}
	}
}