using System.Linq;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Personal
{
	public class PersonalFirstVisitFixture : PersonalFixture
	{
		[Test]
		public void FirstVisit()
		{
			Client = DbSession.Query<Client>().ToList().First(i => i.Patronymic == "неподключенный клиент");
			var internet = Client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Internet);
			var iptv = Client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Iptv);

			Assert.That(Client.Lunched, Is.False);
			Assert.That(Client.Endpoints.Count, Is.EqualTo(0));
			Assert.That(internet.IsActivated, Is.False);
			Assert.That(iptv.IsActivated, Is.False);
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

			date = browser.FindElementByCssSelector("input[name='physicalClient.BirthDate']");
			date.Click();
			popup = browser.FindElementByCssSelector("a.ui-state-default");
			popup.Click();
			//date.SendKeys("18.12.2014");

			var residention = browser.FindElementByCssSelector("input[name='physicalClient.PassportResidention']");
			residention.SendKeys("Паспортно-визовое отделение по району северный гор. Воронежа");

			var address = browser.FindElementByCssSelector("input[name='physicalClient.PassportResidention']");
			address.SendKeys("г. Воронеж, студенческая ул, д12");

			var button = browser.FindElementByCssSelector(".right-block .button");
			button.Click();
			AssertText("успешно");
			DbSession.Clear();
			var client = DbSession.Query<Client>().First(i => i.PhysicalClient.Surname == "Третьяков");
			internet = client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Internet);
			iptv = client.ClientServices.First(i => (ServiceType)i.Service.Id == ServiceType.Iptv);

			Assert.That(client.Lunched, Is.True);
			Assert.That(client.Endpoints.Count, Is.EqualTo(1));
			Assert.That(internet.IsActivated, Is.True);
			Assert.That(iptv.IsActivated, Is.True);
		}
	}
}