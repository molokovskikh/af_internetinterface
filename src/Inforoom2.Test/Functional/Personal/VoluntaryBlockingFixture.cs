using System;
using System.Linq;
using Billing;
using Common.Tools;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Personal
{
	internal class VoluntaryBlockingFixture : PersonalFixture
	{
		private MainBilling _billing;

		[TestCase(arg: true, Description = "Проверка подключения клиенту услуги 'Добровольная блокировка'")]
		public void SetBlockAccountToClient(bool isFree, bool fullCheck = true)
		{
			Assert.IsNotNull(Client.PhysicalClient, "Клиент должен быть подключен");
			SystemTime.Now = () => DateTime.Now;            // Для независимого выполнения каждого тест-кейса

			// Обработать уже созданные платежи/списания клиента
			_billing = GetBilling();
			_billing.SafeProcessPayments();
			_billing.ProcessWriteoffs();
			DbSession.Refresh(Client.PhysicalClient);
			var oldBalance = Client.Balance;                // Сохранить текущий баланс клиента

			if (!isFree) {
				var shift = DateTime.Now.Hour;                // Смещение времени во избежание подключения услуги после 22:00
				SystemTime.Now = () => DateTime.Now.Date.AddHours(-shift);
				Client.PaidDay = false;                       // Для списания абонентской платы
				Client.FreeBlockDays = 0;
				Client.YearCycleDate = SystemTime.Now();      // Чтобы не уставливалось FreeBlockDays = 28
				DbSession.Update(Client);
				DbSession.Flush();
			}

			Open("Personal/Service");
			var btnConnect = browser.FindElementByLinkText("Подключить");
			btnConnect.Click();
			btnConnect = browser.FindElementById("ConnectBtn");
			btnConnect.Click();
			if (!isFree) {
				btnConnect = browser.FindElementByCssSelector(".window .click.ok");
				btnConnect.Click();
			}

			DbSession.Refresh(Client);
			var blockAccountService = Client.ClientServices.FirstOrDefault(cs => (cs.Service as BlockAccountService) != null);
			Assert.IsNotNull(blockAccountService, "\nУ клиента все ещё нет услуги добровольной блокировки");
			Assert.IsTrue(Client.Status.Type == StatusType.VoluntaryBlocking, "\nКлиент не был заблокирован");

			// Обработать новые списания клиента
			_billing.SafeProcessPayments();                 // Для обработки UserWriteOffs
			_billing.ProcessWriteoffs();
			DbSession.Refresh(Client.PhysicalClient);
			if (isFree)
				Assert.AreEqual(0m, oldBalance - Client.Balance, "\nClient.Balance=" + Client.Balance);
			else {
				var userWriteoffs = Client.UserWriteOffs.OrderByDescending(uwo => uwo.Date).ToList();
				var abonentPay = userWriteoffs.FirstOrDefault(uwo => uwo.Comment.Contains("из-за добровольной блокировки"));
				var activatePay = userWriteoffs.FirstOrDefault(uwo => uwo.Comment.Contains("Платеж за активацию услуги"));

				Assert.IsNotNull(abonentPay, "\nАбонентская плата не начислена!");
				Assert.AreNotEqual(0m, abonentPay.Sum, "\nАбонентская плата равна 0.");
				Assert.IsNotNull(activatePay, "\nПлатеж за активацию услуги не начислен!");
				var paySum = abonentPay.Sum + activatePay.Sum;
				Assert.AreEqual(paySum, oldBalance - Client.Balance, "\nClient.Balance=" + Client.Balance);
			}
			if (fullCheck) {
			Open("Personal/Service");
			btnConnect = browser.FindElementByLinkText("Отключить");
            btnConnect.Click();
			var error = String.Format("Услуга может быть деактивирована не ранее {0}.",
				blockAccountService.BeginDate.Value.Date.AddDays(3).ToString("dd.MM.yyyy HH:mm"));
			AssertText(error);
			blockAccountService.BeginDate = SystemTime.Now().Date.AddDays(-3).AddMinutes(-1);
			DbSession.Save(blockAccountService);
			DbSession.Flush();
			Open("Personal/Service");
			btnConnect = browser.FindElementByLinkText("Отключить");
			btnConnect.Click();
			AssertNoText(error);
			btnConnect = browser.FindElementById("DisconnectBtn");
			btnConnect.Click();
			AssertText("Работа возобновлена");
			}

		}

		[Test(Description = "Проверка списания с клиента платы за подключение услуги 'Добровольная блокировка'")]
		public void WriteoffBlockingPayWithClient()
		{
			SetBlockAccountToClient(isFree: false, fullCheck:false);
		}

		[Test(Description = "Проверка списаний с клиента за пользование услугой 'Добровольная блокировка' по истечении бесплатных дней")]
		public void CheckWriteoffsWithClientAfterFreeBlockDays()
		{
			SetBlockAccountToClient(isFree: true, fullCheck: false);

			Client.FreeBlockDays = 0;
			// Чтобы формировались списания: 50 р.(разово) + 3 р.(ежедневно)
			Client.PaidDay = false;
			// Чтобы не уставливалось FreeBlockDays = 28
			Client.YearCycleDate = SystemTime.Now();
			DbSession.Update(Client);
			DbSession.Flush();

			var oldBalance = Client.Balance;
			_billing.ProcessWriteoffs();
			DbSession.Refresh(Client.PhysicalClient);
			Assert.AreEqual(53m, oldBalance - Client.Balance, "\nClient.Balance=" + Client.Balance);

			SystemTime.Now = () => DateTime.Now.AddDays(1);
			// Чтобы формировалось ежедневное списание = 3 р.
			Client.PaidDay = false;
			// Чтобы не уставливалось FreeBlockDays = 28
			Client.YearCycleDate = SystemTime.Now();
			DbSession.Update(Client);
			DbSession.Flush();

			oldBalance = Client.Balance;
			_billing.ProcessWriteoffs();
			DbSession.Refresh(Client.PhysicalClient);
			Assert.AreEqual(3m, oldBalance - Client.Balance, "\nWriteOff!=3");
		}

		[Test(Description = "Проверка отсутствия больших списаний (> 3 р.) у клиента после подключения" +
		                    " услуги 'Добровольная блокировка' при отсутствии бесплатных дней; для задачи №33321")]
		public void CheckNoStrongWriteoffsWithClient()
		{
			// Чтобы был списан разовый платеж за услугу = 50 р.
			SetBlockAccountToClient(isFree: false, fullCheck: false);
			var myClient = DbSession.Get<Client>(Client.Id);

			for (var i = 1; i < 31; i++) {
				var oldBalance = myClient.Balance;
				DbSession.Refresh(myClient);

				SystemTime.Now = () => DateTime.Now.AddDays(i);
				// Чтобы формировалось ежедневное списание = 3 р.
				myClient.PaidDay = false;
				DbSession.Update(myClient);
				DbSession.Flush();
				_billing.ProcessWriteoffs();

				DbSession.Refresh(myClient.PhysicalClient);
				var balanceDiff = oldBalance - myClient.Balance;
				Assert.AreEqual(3m, balanceDiff, "\noldBalance-newBalance=" + balanceDiff);
			}
		}

		[Test(Description = "Проверка отказа в блокировке клиенту с недостаточным балансом; для задачи №33323")]
		public void RefuseClientInBlockAccount()
		{
			// Получить клиента с низким балансом
			var clientMark = ClientCreateHelper.ClientMark.lowBalanceClient.GetDescription();
			var client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Comment == clientMark);
			Assert.IsNotNull(client, "Искомый клиент не найден");
			Assert.AreEqual(StatusType.Worked, client.Status.Type, "Клиент должен быть подключен");

			// Чтобы имелась возм-ть для добровольной блокировки на бесплатные дни
			client.FreeBlockDays = 2;
			DbSession.Update(client);
			DbSession.Flush();

			LoginForClient(client);
			var thisElement = browser.FindElementByLinkText("Услуги");
			thisElement.Click();
			thisElement = browser.FindElementByLinkText("Подключить");
			thisElement.Click();
			thisElement = browser.FindElementById("blockingEndDate");
			thisElement.Clear();
			thisElement.SendKeys(DateTime.Now.AddDays(client.FreeBlockDays + 1).ToShortDateString());
			thisElement = browser.FindElementById("ConnectBtn");
			thisElement.Click();
			thisElement = browser.FindElementByCssSelector(".window .click.ok");
			thisElement.Click();

			AssertText("Недостаточно средств на счете для добровольной блокировки");
			AssertText("Вы можете активировать услугу на бесплатные дни либо пополнить баланс и уже затем перейти к ее активации!");

			// Чтобы возм-ть добровольной блокировки была только после пополнения баланса
			client.FreeBlockDays = 0;
			DbSession.Update(client);
			DbSession.Flush();

			thisElement = browser.FindElementByLinkText("Подключить");
			thisElement.Click();
			thisElement = browser.FindElementById("ConnectBtn");
			thisElement.Click();
			thisElement = browser.FindElementByCssSelector(".window .click.ok");
			thisElement.Click();

			AssertText("Недостаточно средств на счете для добровольной блокировки");
			AssertText("Пополните баланс, чтобы затем перейти к активации услуги!");
		}
	}
}
