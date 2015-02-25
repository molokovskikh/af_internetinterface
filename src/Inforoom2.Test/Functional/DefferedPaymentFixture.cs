using System.Linq;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional
{
	class DefferedPaymentFixture: PersonalFixture
	{
		[Test(Description = "Проверка корректной активации услуги 'Обещанный платеж'")]
		public void ActivateDeferredPayment()
		{
			var client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Patronymic.Contains("заблокированный клиент"));
			Assert.IsNotNull(client, "Искомый клиент не найден");
			Assert.AreEqual(StatusType.NoWorked, client.Status.Type, "Клиент не имеет статус 'Заблокирован'");
			Assert.IsTrue(client.WorkingStartDate.HasValue, "У клиента не выставлена дана подключения");

			client.ShowBalanceWarningPage = true;
			DbSession.Update(client);
			DbSession.Flush();

			LoginForClient(client);
			DbSession.Refresh(client);
			Assert.IsTrue(client.ShowBalanceWarningPage, "Страница 'Warning' не подключена");

			var button = browser.FindElementByLinkText("Услуги");
			button.Click();
			button = browser.FindElementByLinkText("Подключить");
			button.Click();
			button = browser.FindElementByCssSelector("input[value=Подключить]");
			button.Click();
			
			DbSession.Refresh(client);
			Assert.AreEqual(StatusType.Worked, client.Status.Type, "Клиент не имеет статус 'Подключен'");
			Assert.IsFalse(client.ShowBalanceWarningPage, "Страница 'Warning' по-прежнему подключена");
		}
	}
}
