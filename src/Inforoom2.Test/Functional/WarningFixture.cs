using System.Linq;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Billing;

namespace Inforoom2.Test.Functional
{
	public class WarningFixture : PersonalFixture
	{

		[Test(Description = "Тест на визуальное соотвествие")]
		public void VisualTest()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с низким балансом"));
			Assert.That(client.ShowBalanceWarningPage, Is.False);
			var billing = new MainBilling();
			billing.Run();
			Assert.That(client.ShowBalanceWarningPage,Is.True,"Клиенту должна отображаться страница Warning");
		}

	}
}