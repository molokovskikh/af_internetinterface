using System;
using System.Linq;
using Billing;
using Common.Tools;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Functional.infrastructure;
using Inforoom2.Test.Functional.Personal;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional
{
	internal class VoluntaryBlockingFixture : PersonalFixture
	{
		private MainBilling _billing;

		[TestCase(arg: true, Description = "Проверка подключения клиенту услуги 'Добровольная блокировка'")]
		public void SetBlockAccountToClient(bool isFree)
		{
			Assert.IsNotNull(Client.PhysicalClient, "\nЭто не подключенный клиент");

			// Обработать уже созданные платежи/списания клиента
			_billing = GetBilling();
			_billing.SafeProcessPayments();
			_billing.ProcessWriteoffs();
			DbSession.Refresh(Client.PhysicalClient);
			var oldBalance = Client.Balance; // Сохранить текущий баланс клиента

			if (!isFree) {
				SystemTime.Now = () => DateTime.Now.Date.AddHours(10);	// Чтобы избежать подключение услуги после 22:00
				Client.PaidDay = false;												// Для списания абонентской платы
				Client.FreeBlockDays = 0;
				Client.YearCycleDate = SystemTime.Now(); // Чтобы не уставливалось FreeBlockDays = 28
				DbSession.Update(Client);
				DbSession.Flush();
			}

			Open("Personal/Service");
			var btnConnect = browser.FindElementByLinkText("Подключить");
			btnConnect.Click();
			btnConnect = browser.FindElementById("ConnectBtn");
			btnConnect.Click();

			DbSession.Refresh(Client);
			var blockAccountService = Client.ClientServices.FirstOrDefault(cs => (cs.Service as BlockAccountService) != null);
			Assert.IsNotNull(blockAccountService, "\nУ клиента все ещё нет услуги добровольной блокировки");
			Assert.IsTrue(Client.Status.Type == StatusType.VoluntaryBlocking, "\nКлиент не был заблокирован");

			// Обработать новые списания клиента
			_billing.SafeProcessPayments(); // Для обработки UserWriteOffs
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
		}

		[Test(Description = "Проверка списания с клиента платы за подключение услуги 'Добровольная блокировка'")]
		public void WriteoffBlockingPayWithClient()
		{
			SetBlockAccountToClient(isFree: false);
		}

		[Test(Description = "Проверка списаний с клиента за пользование услугой 'Добровольная блокировка' по истечении бесплатных дней")]
		public void CheckWriteoffsWithClientAfterFreeBlockDays()
		{
			SetBlockAccountToClient(isFree: true);

			Client.FreeBlockDays = 0;
			Client.PaidDay = false; // Чтобы формировались списания: 50 р.(разово) + 3 р.(ежедневно)
			Client.YearCycleDate = SystemTime.Now(); // Чтобы не уставливалось FreeBlockDays = 28
			DbSession.Update(Client);
			DbSession.Flush();

			var oldBalance = Client.Balance;
			_billing.ProcessWriteoffs();
			DbSession.Refresh(Client.PhysicalClient);
			Assert.AreEqual(53m, oldBalance - Client.Balance, "\nClient.Balance=" + Client.Balance);

			SystemTime.Now = () => DateTime.Now.AddDays(1);
			Client.PaidDay = false; // Чтобы формировалось ежедневное списание = 3 р.
			Client.YearCycleDate = SystemTime.Now(); // Чтобы не уставливалось FreeBlockDays = 28
			DbSession.Update(Client);
			DbSession.Flush();

			oldBalance = Client.Balance;
			_billing.ProcessWriteoffs();
			DbSession.Refresh(Client.PhysicalClient);
			Console.WriteLine("Client.Balance=" + Client.Balance);
			Assert.AreEqual(3m, oldBalance - Client.Balance, "\nWriteOff!=3");
		}
	}
}