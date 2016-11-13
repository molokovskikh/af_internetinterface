using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure;
using Inforoom2.Test.Infrastructure.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace Inforoom2.Test.Functional.Personal
{
	internal class PlanChangeFixture : BaseFixture
	{
		protected void UpdateDriverSideSystemTime()
		{
			Open($"Home/SetDebugTime?time={SystemTime.Now()}");
			WaitForText($"Время установлено {SystemTime.Now()}");
		}
		public Client CurrentClient;
		public PlanChangerData PlanChangerDataItem;

		/// <summary>
		/// Выставление PlanChanger(а)
		/// </summary>
		/// <param name="timeout">дни до оканчания акции</param>
		/// <param name="updateClient">клиент</param>
		/// <param name="lastTimePlanChangedDays">дни смены тарифа (по умолчанию равны 'дни до оканчания акции')</param>
		public void PlanChangerFixtureOn(int timeout, bool updateClient = true, int lastTimePlanChangedDays = -1)
		{
			CurrentClient = updateClient
				? DbSession.Query<Client>().First(i => i.Comment == ClientCreateHelper.ClientMark.normalClient.GetDescription())
				: CurrentClient;
			lastTimePlanChangedDays = lastTimePlanChangedDays == -1 ? timeout : lastTimePlanChangedDays;
			var tariffTarget = DbSession.Query<Plan>().FirstOrDefault(s => s.Name == "Народный" && s.Price == 300);
			var tariffSpeed = DbSession.Query<Plan>().FirstOrDefault(s => s.Name == "Народный" && s.Price == 600);
			var tariffCheap = DbSession.Query<Plan>().FirstOrDefault(s => s.Name == "Популярный");
			var clientService = new ClientService() {
				Client = CurrentClient,
				Service = DbSession.Query<Service>().FirstOrDefault(s => s.Name == "PlanChanger"),
				IsActivated = true,
				BeginDate = SystemTime.Now().AddDays(timeout)
			};
			CurrentClient.ClientServices.Add(clientService);

			PlanChangerDataItem = new PlanChangerData();
			PlanChangerDataItem.FastPlan = tariffSpeed;
			PlanChangerDataItem.CheapPlan = tariffCheap;
			PlanChangerDataItem.TargetPlan = tariffTarget;
			PlanChangerDataItem.Timeout = timeout;
			PlanChangerDataItem.NotifyDays = 3;
			DbSession.Save(PlanChangerDataItem);
			//tariffTarget.PlanChangerData = PlanChangerDataItem;
			//DbSession.Save(tariffTarget);
			CurrentClient.PhysicalClient.LastTimePlanChanged = DateTime.Now.AddMonths(lastTimePlanChangedDays);
			CurrentClient.PhysicalClient.Plan = tariffTarget;
			DbSession.SaveOrUpdate(CurrentClient);

			DbSession.Flush();
			DbSession.Close();
			DbSession = DbSession.SessionFactory.OpenSession();
			CurrentClient = DbSession.Query<Client>().First(s => s.Id == CurrentClient.Id);
		}

		[Test(Description = "Проверка отработки просроченного PlanChanger сервиса у клиента - незарегистрированного")]
		public void PlanChangerFixtureUserUnregistered()
		{
			PlanChangerFixtureOn(0);
			Open("Personal/Profile");
			AssertText("Вход в личный кабинет");
		}

		[Test(Description = "Проверка отработки PlanChanger сервиса у клиента - с планом, отличным от целевого")]
		public void PlanChangerFixtureUserWithAnotherPlan()
		{
			PlanChangerFixtureOn(0);
			var anotherClient = DbSession.Query<Client>().First(i => i.Comment == ClientCreateHelper.ClientMark.clientWithRegionalPlan.GetDescription());
			LoginForClient(anotherClient);
			Open("/");
			AssertText("НОВОСТИ");
		}

		[Test(Description = "Проверка отработки актуального PlanChanger сервиса у клиента - с целевым планом")]
		public void PlanChangerFixtureUserWithTargetPlanHasTime()
		{
			PlanChangerFixtureOn(100);
			LoginForClient(CurrentClient);
			Open("/");
			AssertText("НОВОСТИ");
		}

		[Test(Description = "Проверка отработки просроченного PlanChanger сервиса у клиента - с целевым планом")]
		public void PlanChangerFixtureUserWithTargetPlanNoTime()
		{
			PlanChangerFixtureOn(0);
			LoginForClient(CurrentClient);
			Open("/");
			AssertNoText("НОВОСТИ");
		}


		[Test(Description = "Проверка отработки просроченного PlanChanger сервиса у клиента - с целевым планом")]
		public void PlanChangerFixtureUserWithTargetPlanMessage()
		{
			var now = DateTime.Now;
			var days = 10;
			SystemTime.Now = () => now;
			PlanChangerFixtureOn(days, lastTimePlanChangedDays:0);
			LoginForClient(CurrentClient);
			var currentPlan = CurrentClient.Plan;
			var targetPlan = CurrentClient.Plan.PlanChangerData.FastPlan;
			for (int i = 1; i <= days; i++)
			{
				SystemTime.Now = () => now.AddDays(i);
				RunBillingProcess();
				DbSession.Refresh(CurrentClient);
				UpdateDriverSideSystemTime();
				Open("warning");
				if (days - i > 3) {
					AssertText("НОВОСТИ");
				} else
				{
					AssertText($"Акционный период на тарифе");
					if (days - i == 0) {
						Assert.IsTrue(CurrentClient.ClientAppeals().Count(s => s.Message.IndexOf("Акционный период на тарифе") != -1) == 1);
						browser.FindElementByCssSelector(".button.unfreeze").Click();
						WaitForText("НЕОБХОДИМО ОПРЕДЕЛИТЬСЯ С ТАРИФОМ");
					}
				}
				Open("/");
				if (i == days) {
					AssertText("НЕОБХОДИМО ОПРЕДЕЛИТЬСЯ С ТАРИФОМ");
				} else {
					AssertText("НОВОСТИ");
				}
			}

			DbSession.Refresh(CurrentClient);
			Assert.That(CurrentClient.Plan == currentPlan);
			SystemTime.Now = () => now.AddDays(11);
			RunBillingProcess();
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.PhysicalClient);
			DbSession.Refresh(CurrentClient.PhysicalClient.Plan);
			Assert.That(CurrentClient.Plan != currentPlan && CurrentClient.Plan == targetPlan);

			SystemTime.Now = () => now;
		}


		[Test(Description = "Проверка отработки просроченного PlanChanger сервиса, при переходе на дешевый тариф")]
		public void PlanChangerFixtureChangePlanForCheap()
		{
			PlanChangerFixtureOn(0);
			LoginForClient(CurrentClient);
			Open("Warning?ip=" + CurrentClient.Endpoints.First(s => !s.Disabled).Ip);
			WaitForText("НЕОБХОДИМО СМЕНИТЬ ТАРИФ");
			browser.FindElementByCssSelector("#changeTariffButtonCheap").Click();
			browser.FindElementByCssSelector(".window .click.ok").Click();
			// клиент должен перейти на дешелый тириф
			AssertText(DbSession.Query<Plan>().First(s => s == PlanChangerDataItem.CheapPlan).Name);
		}

		[Test(Description = "Проверка отработки просроченного PlanChanger сервиса, при переходе на быстрый тариф")]
		public void PlanChangerFixtureChangePlanForFast()
		{
			PlanChangerFixtureOn(0);
			LoginForClient(CurrentClient);
			Open("/");
			browser.FindElementByCssSelector("#changeTariffButtonFast").Click();
			browser.FindElementByCssSelector(".window .click.ok").Click();
			// клиент должен перейти на быстрый тириф
			AssertText(DbSession.Query<Plan>().First(s => s == PlanChangerDataItem.FastPlan).Name);
		}

		[Test(Description = "Проверка возврата переадресации Warning(ом) с главной станицы на выбор тарифа, если отработал PlanChanger (проверка паспортных данных)")]
		public void PlanChangerWithWarningFixtureFromMainToPlanChange()
		{
			CurrentClient = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("без паспортных данных"));
			PlanChangerFixtureOn(0, false);
			LoginForClient(CurrentClient);
			Open("/");
			AssertText("НЕОБХОДИМО СМЕНИТЬ ТАРИФ");
			browser.FindElementByCssSelector("#changeTariffButtonCheap").Click();
			browser.FindElementByCssSelector(".window .click.ok").Click();
			// клиент должен перейти на дешелый тириф
			AssertText("Тариф успешно изменен");
			AssertText(DbSession.Query<Plan>().First(s => s == PlanChangerDataItem.CheapPlan).Name);
			Open("/Personal/Plans");
			AssertText("У вас не заполнены паспортные данные");
			Open("/Personal/Payment");
			AssertText("У вас не было платежей");
			Open("/Personal/Service");
			AssertText("У вас не заполнены паспортные данные");
			Open("/Personal/Bonus");
			AssertText("У вас не заполнены паспортные данные");
		}

		[Test(Description = "Проверка конфликта Warning с PlanChanger, отсутствие паспоротных данных")]
		public void PlanChangerWithWarningFixturePassportData()
		{
			CurrentClient = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("без паспортных данных"));
			PlanChangerFixtureOn(0, false);
			LoginForClient(CurrentClient);
			Open("/");
			AssertText("НЕОБХОДИМО СМЕНИТЬ ТАРИФ");
			browser.FindElementByCssSelector("#changeTariffButtonCheap").Click();
			browser.FindElementByCssSelector(".window .click.ok").Click();
			// клиент должен перейти на дешелый тириф
			AssertText("Тариф успешно изменен");
			var endpoint = CurrentClient.Endpoints.FirstOrDefault();
			var lease = DbSession.Query<Lease>().FirstOrDefault(i => i.Endpoint == endpoint);
			var ip = lease?.Ip?.ToString();

			Open("/");
			AssertText("НОВОСТИ");
			Open($"Warning?ip={ip}");
			AssertText("У вас не заполнены паспортные данные");
			Css(".warning").Click();
			var textbox = browser.FindElement(By.CssSelector("#physicalClient_PassportNumber"));
			textbox.SendKeys("7121551");
			var button = browser.FindElement(By.CssSelector("form input.button"));
			button.Click();
			Open($"Warning?ip={ip}");
			AssertText("У вас не заполнены паспортные данные");
			Css(".warning").Click();
			var date = browser.FindElementByCssSelector("input[name='physicalClient.BirthDate']");
			date.Click();
			var popup = browser.FindElementByCssSelector("a.ui-state-default");
			popup.Click();
			button = browser.FindElement(By.CssSelector("form input.button"));
			button.Click();
			Open($"Warning?ip={ip}");
			AssertText("Для заполнения недостающих паспортных данных необходимо обратиться в офис компании");
			Css(".warning").Click();
			AssertText("НОВОСТИ");
		}

		[Test(Description = "Проверка конфликта Warning с PlanChanger, низкий баланс")]
		public void PlanChangerWithWarningFixtureNegartiveBalanceToNormalBalance()
		{
			CurrentClient = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("нормальный клиент"));
			PlanChangerFixtureOn(0, false);
			LoginForClient(CurrentClient);
			Open("/");
			CurrentClient.PhysicalClient.Balance = -5;
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcessWriteoffs(CurrentClient);
			AssertText("Ваш лицевой счет заблокирован за неуплату, для разблокировки необходимо внести");

			DbSession.Refresh(CurrentClient);
			CurrentClient.PhysicalClient.Balance = 500;
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcessWriteoffs(CurrentClient);
			Open("/");
			AssertText("НЕОБХОДИМО СМЕНИТЬ ТАРИФ");
			browser.FindElementByCssSelector("#changeTariffButtonCheap").Click();
			browser.FindElementByCssSelector(".window .click.ok").Click();
			// клиент должен перейти на дешелый тириф
			AssertText(DbSession.Query<Plan>().First(s => s == PlanChangerDataItem.CheapPlan).Name);
		}

		[Test(Description = "Проверка конфликта Warning с PlanChanger, при активации услуги 'Обещанный платеж'")]
		public void PlanChangerWithWarningFixtureDeferredPayment()
		{
			CurrentClient = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("нормальный клиент"));
			PlanChangerFixtureOn(0, false);
			CurrentClient.SetStatus(StatusType.NoWorked, DbSession);
			CurrentClient.Balance = -1;
			var payment = new Payment() {
				Client = CurrentClient,
				Sum = 0,
				PaidOn = SystemTime.Now().AddDays(-2),
				RecievedOn = SystemTime.Now().AddDays(-1)
			};
			CurrentClient.Payments.Add(payment);
			DbSession.Save(payment);
			Assert.IsNotNull(CurrentClient, "Искомый клиент не найден");
			Assert.AreEqual(StatusType.NoWorked, CurrentClient.Status.Type, "Клиент не имеет статус 'Заблокирован'");
			Assert.IsTrue(CurrentClient.WorkingStartDate.HasValue, "У клиента не выставлена дата подключения");
			DbSession.Update(CurrentClient);
			DbSession.Flush();

			LoginForClient(CurrentClient);
			DbSession.Refresh(CurrentClient);

			LoginForClient(CurrentClient);
			Open("/");

			AssertText("Ваш лицевой счет заблокирован за неуплату, для разблокировки необходимо внести");

			var button = browser.FindElement(By.CssSelector("form input.button"));
			button.Click();

			button = browser.FindElementByLinkText("Подключить");
			button.Click();
			button = browser.FindElementByCssSelector("input[value=Подключить]");
			button.Click();
			AssertText("Услуга \"Обещанный платеж\" активирована на период");
			Open("Personal/Profile");
			Open("/");
			AssertText("НЕОБХОДИМО СМЕНИТЬ ТАРИФ");
			browser.FindElementByCssSelector("#changeTariffButtonFast").Click();
			//	browser.FindElementByCssSelector("#changeTariffButtonCheap").Click();
			browser.FindElementByCssSelector(".window .click.ok").Click();
			// клиент должен перейти на дешелый тириф
			AssertText(DbSession.Query<Plan>().First(s => s == PlanChangerDataItem.FastPlan).Name);
		}
	}
}