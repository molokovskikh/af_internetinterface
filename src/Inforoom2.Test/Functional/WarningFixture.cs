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

		[Test(Description = "Тест на визуальное соотвествие"),Ignore]
		public void VisualTest()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с низким балансом"));
			Assert.That(client.ShowBalanceWarningPage, Is.False);
			MainBilling.InitActiveRecord();
			var billing = new MainBilling();
			billing.ProcessWriteoffs();
			Assert.That(client.ShowBalanceWarningPage,Is.True,"Клиенту не отображается страница Warning");
		}

	}
}