﻿using System;
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
		[SetUp]
		public void OnStart()
		{
			SystemTime.Now = () => DateTime.Now;
		}

		[TearDown]
		public void OnFinish()
		{
			SystemTime.Now = () => DateTime.Now;
		}
		/// <summary>
		/// При обычном Flush возникала ошибка, заменил на закрытие сесии - помогло.
		/// </summary>
		private void UpdateDBSession()
		{
			DbSession.Flush();
			DbSession.Close();
			DbSession = DbSession.SessionFactory.OpenSession();
			Client = DbSession.Query<Client>().First(s => s.Id == Client.Id);
		}

		/// <summary>
		/// Проверка подключения клиенту услуги 'Добровольная блокировка'
		/// </summary>
		/// <param name="isFree">Бесплатное подключение услуги</param>
		/// <param name="fullCheck">Включить проверку после отключения услуги</param>
		[TestCase(arg: true, Description = "Проверка подключения клиенту услуги 'Добровольная блокировка'")]
		public void SetBlockAccountToClient(bool isFree, bool fullCheck = true, bool activationFallInMinuse = false)
		{
			Assert.IsNotNull(Client.PhysicalClient, "Клиент должен быть подключен");
			SystemTime.Now = () => DateTime.Now; // Для независимого выполнения каждого тест-кейса

			// Обработать уже созданные платежи/списания клиента
			_billing = GetBilling();
			_billing.SafeProcessPayments();
			_billing.ProcessWriteoffs();

			UpdateDBSession();

			DbSession.Refresh(Client.PhysicalClient);

			var oldBalance = Client.Balance; // Сохранить текущий баланс клиента
			var oldBonnaWriteOff = Client.GetSumForRegularWriteOff(); //текущая абон плата
			//отмечаем текущий день, как неоплаченный
			Client.PaidDay = false;
			if (!isFree) {
				Client.FreeBlockDays = 0; // Для списания абонентской платы
				Client.YearCycleDate = SystemTime.Now(); // Чтобы не уставливалось FreeBlockDays = 28
			}
			DbSession.Update(Client);
			DbSession.Flush();

			// подключение услугу Добровольной блокировки
			Open("Personal/Service");
			var btnConnect = browser.FindElementByLinkText("Подключить");
			btnConnect.Click();
			btnConnect = browser.FindElementById("ConnectBtn");
			btnConnect.Click();
			if (!isFree) {
				btnConnect = browser.FindElementByCssSelector(".window .click.ok");
				btnConnect.Click();
			}

			if (activationFallInMinuse) {

				var saleSettings = DbSession.Query<SaleSettings>().FirstOrDefault();
				Assert.IsTrue(Client.Discount == saleSettings.MaxSale);
				UpdateDBSession();
				Client.Balance = 0;
				DbSession.Save(Client);
			}
			UpdateDBSession();

			var blockAccountService = Client.ClientServices.FirstOrDefault(cs => (cs.Service as BlockAccountService) != null);
			Assert.IsNotNull(blockAccountService);
			Assert.IsTrue(Client.Status.Type == StatusType.Worked);

			// Обработать новые списания клиента
			RunBillingProcess();
			UpdateDBSession();
			DbSession.Refresh(Client.PhysicalClient);
			DbSession.Refresh(Client);
			blockAccountService = Client.ClientServices.FirstOrDefault(cs => (cs.Service as BlockAccountService) != null);

			Assert.IsNotNull(blockAccountService, "\nУ клиента подключена добровольная блокировка");
			Assert.IsTrue(Client.Status.Type == StatusType.VoluntaryBlocking, "\nКлиент не был заблокирован");

			if (activationFallInMinuse) {
				Assert.IsNotNull(Client.Balance == - (oldBonnaWriteOff + 50));
				RunBillingProcess();
				UpdateDBSession();
				Assert.IsTrue(Client.Status.Type == StatusType.NoWorked, "\nКлиент был заблокирован");
				return;
			}

			if (isFree)
				Assert.AreEqual(oldBonnaWriteOff, oldBalance - Client.Balance, "\nClient.Balance=" + Client.Balance);
			else { 
				var bonnaPay = Client.WriteOffs.OrderByDescending(uwo => uwo.WriteOffDate).FirstOrDefault();
				var activatePay = Client.UserWriteOffs.OrderByDescending(uwo => uwo.Date).FirstOrDefault(uwo => uwo.Comment.Contains("Платеж за активацию услуги"));

				Assert.IsNotNull(bonnaPay, "\nАбонентская плата не начислена!");
				Assert.AreNotEqual(0m, bonnaPay.WriteOffSum, "\nАбонентская плата равна 0.");
				Assert.IsNotNull(activatePay, "\nПлатеж за активацию услуги не начислен!");
				var paySum = bonnaPay.WriteOffSum + activatePay.Sum;
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

				UpdateDBSession();
				Open("Personal/Service");
				btnConnect = browser.FindElementByLinkText("Отключить");
				btnConnect.Click();
				AssertNoText(error);
				btnConnect = browser.FindElementById("DisconnectBtn");
				btnConnect.Click();
				AssertText("Работа возобновлена");

				UpdateDBSession();
				blockAccountService = Client.ClientServices.FirstOrDefault(cs => (cs.Service as BlockAccountService) != null);
				Assert.IsNull(blockAccountService, "\nУ клиента все ещё нет услуги добровольной блокировки");
				Assert.IsTrue(Client.Status.Type == StatusType.Worked, "\nКлиент не был заблокирован");
			}
		}

		[Test(Description = "Проверка списания с клиента платы за подключение услуги 'Добровольная блокировка'")]
		public void WriteoffBlockingPayWithClient()
		{
			SetBlockAccountToClient(isFree: false, fullCheck: false);
		}
		[Test(Description = "Проверка отсутствия скидки после выхода  в минус после выключения услуги")]
		public void WriteoffBlockingPayWithClientDiscount()
		{
			var saleSettings = DbSession.Query<SaleSettings>().FirstOrDefault();
			Client.Discount = saleSettings.MaxSale;
			DbSession.Save(Client);
			UpdateDBSession();
			SetBlockAccountToClient(isFree: false, fullCheck: false, activationFallInMinuse: true);
			DbSession.Refresh(Client);
			Assert.IsTrue(Client.Discount == 0); 
		}

		[Test(Description ="Проверка списаний с клиента за пользование услугой 'Добровольная блокировка' по истечении бесплатных дней")]
		public void CheckWriteoffsWithClientAfterFreeBlockDays()
		{
			// проверка абон платы, должна быть больше, чем списания с "Добровольной блокировкой"
			var oldBonnaWriteOff = Client.GetSumForRegularWriteOff();
			Assert.IsTrue(oldBonnaWriteOff > 3);
			// добавление услуги "Добровольной блокировкой" клиенту с бесплатными блокировочными днями
			SetBlockAccountToClient(isFree: true, fullCheck: false);

			// зануление бесплатных блокировочных дней
			// должны формироваться списания: 50 р.(разово за подключение услуги после "бесплатных блокировочных дней") + 3 р.(ежедневно)
			UpdateDBSession();
			Client.FreeBlockDays = 0;
			Client.PaidDay = false;
			// Чтобы не уставливалось FreeBlockDays = 28
			Client.YearCycleDate = SystemTime.Now();
			DbSession.Update(Client);
			DbSession.Flush();

			var oldBalance = Client.Balance;

			_billing.ProcessWriteoffs();

			UpdateDBSession();
			DbSession.Refresh(Client.PhysicalClient);

			//баланс уменьшился на 50 (разово)
			Assert.AreEqual(50,
				oldBalance - Client.Balance, "\nClient.Balance=" + Client.Balance);

			SystemTime.Now = () => DateTime.Now.AddDays(1);
			// Чтобы формировалось ежедневное списание = 3 р.
			Client.PaidDay = false;
			// Чтобы не уставливалось FreeBlockDays = 28
			Client.YearCycleDate = SystemTime.Now();
			DbSession.Update(Client);
			DbSession.Flush();

			oldBalance = Client.Balance;
			_billing.ProcessWriteoffs();

			UpdateDBSession();
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
		public void ExitBlockAccountBillingAfter()
		{
			// Получить клиента с низким балансом
			var clientMark = ClientCreateHelper.ClientMark.lowBalanceClient.GetDescription();
			var client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Comment == clientMark);
			Assert.IsNotNull(client, "Искомый клиент не найден");
			Assert.AreEqual(StatusType.Worked, client.Status.Type, "Клиент должен быть подключен");

			var balance = client.Balance;
			var sumForRegularWriteOff = client.GetSumForRegularWriteOff();

			Assert.True(balance - sumForRegularWriteOff > 0);
			Assert.True(!client.Disabled);

			var internetServiceId = Service.GetIdByType(typeof(Internet));
			var dateToday = DateTime.Now;
			SystemTime.Now = () => dateToday;

			// Чтобы имелась возм-ть для добровольной блокировки на бесплатные дни
			client.FreeBlockDays = 28;
			client.PaidDay = false;
			DbSession.Update(client);
			DbSession.Flush();
			LoginForClient(client);
			var thisElement = browser.FindElementByLinkText("Услуги");
			thisElement.Click();
			thisElement = browser.FindElementByLinkText("Подключить");
			thisElement.Click();
			thisElement = browser.FindElementById("weeksCount");
			thisElement.Clear();
			thisElement.SendKeys("1");
			thisElement = browser.FindElementById("ConnectBtn");
			thisElement.Click();
			WaitForVisibleCss(".notification", 20);
			WaitForText("активирована на период", 10);

			RunBillingProcess();

			UpdateDBSession();
			client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Id == client.Id);
			//списание абон.платы при услуге 'Добровольная блокировка', если клиент продолжит пользоваться услугой, биллинг спишет сумму по нему
			var writeoff = client.WriteOffs;
			Assert.True(writeoff.Count == 1);
			Assert.True(writeoff.FirstOrDefault().WriteOffSum == sumForRegularWriteOff);
			Assert.True(balance == client.Balance + sumForRegularWriteOff);
			Assert.True(client.Disabled);
			Assert.True(client.ClientServices.Any(s => s.Service.Id == internetServiceId && s.IsActivated));
			//отказываемся от услуги после списаний биллингом абон.платы, проверяем, что абон. плата по услуге списана, а обычная нет 

			RunBillingProcess();
			
			UpdateDBSession();
			client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Id == client.Id);

			writeoff = client.WriteOffs;
			Assert.True(writeoff.Count == 1);
			Assert.True(balance - sumForRegularWriteOff == client.Balance);
			Assert.True(client.Disabled);
			Assert.True(!client.ClientServices.Any(s => s.Service.Id == internetServiceId && s.IsActivated));


			thisElement = browser.FindElementByCssSelector(".disposalbg");
			thisElement.Click();
			WaitForVisibleCss(".button.unfreeze", 15);
			thisElement = browser.FindElementByCssSelector(".button.unfreeze");
			thisElement.Click();

			UpdateDBSession();
			client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Id == client.Id);

			writeoff = client.WriteOffs;
			Assert.True(writeoff.Count == 1);
			Assert.True(balance - sumForRegularWriteOff == client.Balance);
			Assert.True(!client.Disabled);
			Assert.True(client.ClientServices.Any(s => s.Service.Id == internetServiceId && s.IsActivated));
		}

		[Test(Description = "Проверка отказа в блокировке клиенту с недостаточным балансом; для задачи №33323")]
		public void ExitBlockAccountBillingBefore()
		{
			// Получить клиента с низким балансом
			var clientMark = ClientCreateHelper.ClientMark.lowBalanceClient.GetDescription();
			var client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Comment == clientMark);
			Assert.IsNotNull(client, "Искомый клиент не найден");
			Assert.AreEqual(StatusType.Worked, client.Status.Type, "Клиент должен быть подключен");

			var balance = client.Balance;
			client.PaidDay = false;
			var sumForRegularWriteOff = client.GetSumForRegularWriteOff();

			Assert.True(balance - sumForRegularWriteOff > 0);
			Assert.True(!client.Disabled);

			var internetServiceId = Service.GetIdByType(typeof(Internet));
			var dateToday = DateTime.Now;
			SystemTime.Now = () => dateToday;

			// Чтобы имелась возм-ть для добровольной блокировки на бесплатные дни
			client.FreeBlockDays = 28;
			DbSession.Update(client);
			DbSession.Flush();
			LoginForClient(client);
			var thisElement = browser.FindElementByLinkText("Услуги");
			thisElement.Click();
			thisElement = browser.FindElementByLinkText("Подключить");
			thisElement.Click();
			thisElement = browser.FindElementById("weeksCount");
			thisElement.Clear();
			thisElement.SendKeys("1");
			thisElement = browser.FindElementById("ConnectBtn");
			thisElement.Click();
			WaitForVisibleCss(".notification",20);
			WaitForText("активирована на период", 10);
			UpdateDBSession();

			RunBillingProcess();

			UpdateDBSession();
			client = DbSession.Query<Client>().FirstOrDefault(s => s.Id == client.Id);
			//списание абон.платы при услуге 'Добровольная блокировка', если клиент продолжит пользоваться услугой, биллинг спишет сумму по нему
			var writeoff = client.WriteOffs;
			Assert.True(writeoff.Count == 1);
			Assert.True(balance == client.Balance + sumForRegularWriteOff);
			Assert.True(client.Disabled);
			Assert.True(client.ClientServices.Any(s => s.Service.Id == internetServiceId && s.IsActivated));
			//отказываемся от услуги до списаний биллингом абон.платы, проверяем, что абон. плата будет списана как обычно (на основании подключенного интернета), 
			// а созданное списание абон.платы при услуге 'Добровольная блокировка' отмечено как обработанное биллингом и Игнорируемое.

			thisElement = browser.FindElementByCssSelector(".disposalbg");
			thisElement.Click();
			WaitForVisibleCss(".button.unfreeze",15);
			thisElement = browser.FindElementByCssSelector(".button.unfreeze");
			thisElement.Click();

			UpdateDBSession();
			client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Id == client.Id);

			writeoff = client.WriteOffs;
			Assert.True(writeoff.Count == 1);
			Assert.True(balance == client.Balance + sumForRegularWriteOff);
			Assert.True(!client.Disabled);
			Assert.True(client.ClientServices.Any(s => s.Service.Id == internetServiceId && s.IsActivated));

			RunBillingProcess();
			UpdateDBSession();
			client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Id == client.Id);

			Assert.True(balance - sumForRegularWriteOff == client.Balance);
			Assert.True(!client.Disabled);

		}

		[Test(Description = "Проверка отказа в блокировке клиенту с недостаточным балансом; для задачи №33323")]
		public void RefuseClientInBlockAccount()
		{
			// Получить клиента с низким балансом
			var clientMark = ClientCreateHelper.ClientMark.lowBalanceClient.GetDescription();
			var client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Comment == clientMark);
			Assert.IsNotNull(client, "Искомый клиент не найден");
			Assert.AreEqual(StatusType.Worked, client.Status.Type, "Клиент должен быть подключен");

			var dateToday = DateTime.Now;
			SystemTime.Now = () => dateToday;

			// Чтобы имелась возм-ть для добровольной блокировки на бесплатные дни
			client.FreeBlockDays = 2;
			DbSession.Update(client);
			DbSession.Flush();

			LoginForClient(client);
			var thisElement = browser.FindElementByLinkText("Услуги");
			thisElement.Click();
			thisElement = browser.FindElementByLinkText("Подключить");
			thisElement.Click();
			thisElement = browser.FindElementById("weeksCount");
			thisElement.Clear();
			thisElement.SendKeys((client.FreeBlockDays + 1).ToString());
			thisElement = browser.FindElementById("ConnectBtn");
			thisElement.Click();
			thisElement = browser.FindElementByCssSelector(".window .click.ok");
			thisElement.Click();
			WaitForVisibleCss("div");
			WaitForText("Недостаточно средств на счете для добровольной блокировки", 20);
			AssertText(
				"Вы можете активировать услугу на бесплатные дни либо пополнить баланс и уже затем перейти к ее активации!");

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

			var countOfDays = 7;
			client.FreeBlockDays = 2;
			var countOfWriteOffs = countOfDays - client.FreeBlockDays;
			var userRegularWriteOff = client.GetSumForRegularWriteOff();
			client.PhysicalClient.Balance = 50 + client.GetSumForRegularWriteOff() 
				+ 3 * countOfWriteOffs;
			client.PaidDay = false;
			DbSession.Update(client);
			DbSession.Update(client.PhysicalClient);
			DbSession.Flush();
			Open("Personal/Service");

			thisElement = browser.FindElementByLinkText("Подключить");
			thisElement.Click();
			thisElement = browser.FindElementById("ConnectBtn");
			thisElement.Click();
			thisElement = browser.FindElementByCssSelector(".window .click.ok");
			thisElement.Click();

			AssertText("Услуга \"Добровольная блокировка\" активирована на период");


			//после СПИСАНИЙ счет должен быть уменьшен на одну полную абоненсткую плату
			RunBillingProcess();
			UpdateDBSession();
			client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Id == client.Id);
			DbSession.Refresh(client);
			Assert.IsTrue(client.Balance == 50 + 3 * countOfWriteOffs);

			var indexFreeBlock = client.FreeBlockDays;
			for (int i = 1; i <= countOfDays + 2; i++) {
				SystemTime.Now = () => DateTime.Now.AddDays(i);
				client.PaidDay = false;
				DbSession.Update(client);
				DbSession.Flush();
				RunBillingProcess();
				UpdateDBSession();
				client = DbSession.Query<Client>().ToList().FirstOrDefault(c => c.Id == client.Id);
				DbSession.Refresh(client);
				if (client.Status.Type == StatusType.VoluntaryBlocking) {
					if (indexFreeBlock - i > 0) {
						Assert.IsTrue(indexFreeBlock - i == client.FreeBlockDays && client.Balance == 50 + 3*countOfWriteOffs);
					} else {
						if (indexFreeBlock - i == 0) {
							Assert.IsTrue(client.Balance == 3*(countOfWriteOffs));
						} else {
							Assert.IsTrue(client.Balance == 3*(countOfDays - i));
						}
					}
				} else {
					if (i == countOfDays) {
						Assert.IsTrue(client.Status.Type == StatusType.Worked && client.Balance == 3);
					} else {
						//обходится несколько раз, чтобы убедиться, что ниже описаний не пойдут
						Assert.IsTrue(client.Status.Type == StatusType.NoWorked && client.Balance == 3 - userRegularWriteOff);
					}
				}
				SystemTime.Now = () => dateToday;
			}
		}
	}
}