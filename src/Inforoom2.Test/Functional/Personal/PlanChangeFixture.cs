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

namespace Inforoom2.Test.Functional.Personal
{
	internal class PlanChangeFixture : BaseFixture
	{
		public Client CurrentClient;
		public PlanChangerData PlanChangerDataItem;

		public void PlanChangerFixtureOn(int Timeout)
		{
			CurrentClient = DbSession.Query<Client>().First(i => i.Comment == ClientCreateHelper.ClientMark.normalClient.GetDescription());

			var tariffTarget = DbSession.Query<Plan>().FirstOrDefault(s => s.Name == "Народный" && s.Price == 300);
			var tariffSpeed = DbSession.Query<Plan>().FirstOrDefault(s => s.Name == "Народный" && s.Price == 600);
			var tariffCheap = DbSession.Query<Plan>().FirstOrDefault(s => s.Name == "Популярный"); 
			var clientService = new ClientService() {
				Client = CurrentClient,
				Service = DbSession.Query<Service>().FirstOrDefault(s => s.Name == "PlanChanger"),
				IsActivated = true,
				BeginDate = SystemTime.Now().AddDays(Timeout)
			}; 
			CurrentClient.ClientServices.Add(clientService);

			PlanChangerDataItem = new PlanChangerData();
			PlanChangerDataItem.FastPlan = tariffSpeed;
			PlanChangerDataItem.CheapPlan = tariffCheap;
			PlanChangerDataItem.TargetPlan = tariffTarget;
			PlanChangerDataItem.Timeout = Timeout;
			DbSession.Save(PlanChangerDataItem);
			CurrentClient.PhysicalClient.LastTimePlanChanged = DateTime.Now.AddMonths(-3);
			CurrentClient.PhysicalClient.Plan = tariffTarget;
			DbSession.SaveOrUpdate(CurrentClient);
			DbSession.Flush();
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
			AssertText("ЛИЧНЫЙ КАБИНЕТ: ПРОФИЛЬ");
		}

		[Test(Description = "Проверка отработки актуального PlanChanger сервиса у клиента - с целевым планом")]
		public void PlanChangerFixtureUserWithTargetPlanHasTime()
		{
			PlanChangerFixtureOn(100);
			LoginForClient(CurrentClient);
			AssertText("ЛИЧНЫЙ КАБИНЕТ: ПРОФИЛЬ");
		}

		[Test(Description = "Проверка отработки просроченного PlanChanger сервиса у клиента - с целевым планом")]
		public void PlanChangerFixtureUserWithTargetPlanNoTime()
		{
			PlanChangerFixtureOn(0);
			LoginForClient(CurrentClient);
			AssertText("НЕОБХОДИМО СМЕНИТЬ ТАРИФ");
		}


		[Test(Description = "Проверка отработки просроченного PlanChanger сервиса, при переходе на дешевый тариф")]
		public void PlanChangerFixtureChangePlanForCheap()
		{
			PlanChangerFixtureOn(0);
			LoginForClient(CurrentClient);
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
			browser.FindElementByCssSelector("#changeTariffButtonFast").Click();
			browser.FindElementByCssSelector(".window .click.ok").Click();
			// клиент должен перейти на быстрый тириф
			AssertText(DbSession.Query<Plan>().First(s => s == PlanChangerDataItem.FastPlan).Name);
		}
	}
}