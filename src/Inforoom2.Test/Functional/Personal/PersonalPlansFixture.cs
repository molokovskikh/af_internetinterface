using System.Linq;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure;
using Inforoom2.Test.Infrastructure.Helpers;
using NHibernate.Criterion;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace Inforoom2.Test.Functional.Personal
{
	public class PersonalPlansFixture : PersonalFixture
	{
		/// <summary>
		/// Клиент - Кузнецов: положительный баланс
		/// </summary>
		[SetUp]
		public void Setup()
		{
			Open("Personal/Plans");
			AssertText("Текущий тарифный план");
		}

		[Test(Description = "Тест на визуальное соотвествие")]
		public void VisualTest()
		{
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'Популярный')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var status = row.FindElement(By.CssSelector(".paragraph")).Text;
			Assert.That(status, Is.EqualTo("Текущий"), "На странице тарифный план клиента должен обозначаться строкой - текущий");
		}

		[Test(Description = "Смена тарифа при положительном балансе")]
		public void ChangePlan()
		{
			var planFrom = Client.Plan;
			var planTo = DbSession.Query<Plan>().First(i => i.Name == "Оптимальный");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + planTo.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("input.connectfee"));
			button.Click();
			var confirm = browser.FindElementByCssSelector(".window .click.ok");
			confirm.Click();

			targetPlan = browser.FindElementByXPath("//td[contains(.,'" + planTo.Name + "')]");
			row = targetPlan.FindElement(By.XPath(".."));
			var status = row.FindElement(By.CssSelector(".paragraph")).Text;
			Assert.That(status, Is.EqualTo("Текущий"), "На странице тарифный план клиента должен обозначаться строкой - текущий");

			var client = Client;
			DbSession.Refresh(client);
			var appeal = client.Appeals.FirstOrDefault(i => i.Message.Contains("Изменение тарифа"));
			var writeoff = client.UserWriteOffs.FirstOrDefault(i => i.Comment.Contains("Изменение тарифа, старый"));
			Assert.That(writeoff, Is.Not.Null, "У клиента должно быть денежное списание за смену тарифа");
			Assert.That(writeoff.Comment, Does.Contain(planTo.Name), "В тексте описания денежного списания должно присутсвовать наименование тарифа,на который перешли");
			Assert.That(writeoff.Comment, Does.Contain(planFrom.Name), "В тексте описания денежного списания должно присутсвовать наименование тарифа,который сменили");
			Assert.That(appeal.Message, Does.Contain(planFrom.Name), "В тексте информационного сообщения о клиенте должно присутсвовать наименование тарифа,который сменили");
			Assert.That(appeal.Message, Does.Contain(planTo.Name), "В тексте информационного сообщения о клиенте должно присутсвовать наименование тарифа,на который перешли");
			Assert.That(appeal, Is.Not.Null, "У клиента должно быть информационно сообщение о смене тарифа");
			Assert.That(appeal.Message, Does.Contain("(" + planFrom.Price + ")"), "Информационное сообщение о клиенте должно содержать стоимость тарифа,который сменили");
			Assert.That(appeal.Message, Does.Contain(planTo.Name), "Информационное сообщение о клиенте должно содержать наименование тарифа,на который перешли");
			Assert.That(appeal.Message, Does.Contain("(" + planTo.Price + ")"), "Информационное сообщение о клиенте должно содержать стоимость тарифа,на который перешли");
			Assert.That(appeal.Message, Does.Contain(writeoff.Sum.ToString()), "Информационное сообщение о клиенте должно содержать стоимость смены тарифа");
		}


		[Test(Description = "Отображение тарифов по регионам")]
		public void PlanRegion()
		{
			var Сlient = DbSession.Query<Client>().First(i => i.Comment == ClientCreateHelper.ClientMark.clientWithRegionalPlan.GetDescription());
			LoginForClient(Сlient);
			Open("Personal/Plans");
			var plan = DbSession.Query<Plan>().First(i => i.Name == "50 на 50");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + plan.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var planRegion = row.FindElement(By.CssSelector(".tariffscost")).Text;
			Assert.That(planRegion, Is.EqualTo(plan.Name), "Клиенту должен отображаться региональный план 50 на 50");
			AssertNoText("Старт");
		}


		[Test(Description = "Отображение тарифов по региону дома")]
		public void PlanRegionHouse()
		{
			//клиент у которого регион дома и улицы разный
			var Client = DbSession.Query<Client>().First(i => i.Comment == ClientCreateHelper.ClientMark.clientWithDifferentRegionHouse.GetDescription());
			LoginForClient(Client);
			Open("Personal/Plans");
			//видит тариф региона дома
			AssertText("Старт");
			//не видит тариф региона улицы
			AssertNoText("50 на 50");
			var regionStreet = Client.PhysicalClient.Address.House.Street.Region;
			var regionHouse = Client.PhysicalClient.Address.House.Region;
			Assert.That(regionStreet, Is.Not.EqualTo(regionHouse), "Регион дома не дожен соответствовать региону улицы");
			//клиент без региона дома
			Client.PhysicalClient.Address.House.Region = null;
			DbSession.Save(Client.PhysicalClient.Address.House);
			DbSession.Flush();
			Open("Personal/Plans");
			//видит тариф региона улицы
			AssertText("50 на 50");
			//не видит тариф региона дома
			AssertNoText("Старт");
			regionStreet = Client.PhysicalClient.Address.House.Street.Region;
			regionHouse = Client.PhysicalClient.Address.House.Region;
			Assert.That(regionStreet, Is.Not.EqualTo(regionHouse), "Регион дома не дожен соответствовать региону улицы");
		}


		[Test(Description = "Не возможна смена тарифа при отрицательном балансе.")]
		public void PlanLowBalance()
		{
			var Client = DbSession.Query<Client>().First(i => i.Comment == ClientCreateHelper.ClientMark.lowBalanceClient.GetDescription());
			LoginForClient(Client);
			Open("Personal/Plans");
			var planTo = DbSession.Query<Plan>().First(i => i.Name == "Оптимальный");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + planTo.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("input.connectfee"));
			button.Click();
			var confirm = browser.FindElementByCssSelector(".window .click.ok");
			confirm.Click();
			AssertText("Не достаточно средств для смены тарифного плана");
			DbSession.Refresh(Client);
			var appeal = Client.Appeals.FirstOrDefault(i => i.Message.Contains("Изменение тарифа"));
			Assert.That(appeal, Is.Null, "Информационное сообщение о пользователе должно быть пустым");
			var writeoff = Client.UserWriteOffs.FirstOrDefault(i => i.Comment.Contains("Изменение тарифа, старый"));
			Assert.That(writeoff, Is.Null, "У клиента не должно быть списания денежных средств за смену тарифа");
		}

		[Test(Description = "Новому клиенту не доступна смена тарифа первые несколько месяцев.")]
		public void PlanRecentClient()
		{
			var Client = DbSession.Query<Client>().First(i => i.Comment == ClientCreateHelper.ClientMark.recentClient.GetDescription());
			LoginForClient(Client);
			Open("Personal/Plans");
			AssertText("Вы пока не можете сменить тарифный план, т.к. подключились к нам менее 2-х месяцев назад.");
			var button = browser.FindElementsByCssSelector(".connectfee");
			Assert.That(button.Count, Is.EqualTo(0), "На странице не должны отображаться кнопки для смены тарифа");
			Client.Plan.StoppageMonths = 3;
			DbSession.Save(Client.Plan);
			DbSession.Flush();
			Open("Personal/Plans");
			AssertText("Вы пока не можете сменить тарифный план, т.к. подключились к нам менее 3-х месяцев назад.");
			button = browser.FindElementsByCssSelector(".connectfee");
			Assert.That(button.Count, Is.EqualTo(0), "На странице не должны отображаться кнопки для смены тарифа");
			Client.Plan.StoppageMonths = 0;
			DbSession.Save(Client.Plan);
			DbSession.Flush();
			Open("Personal/Plans");
			AssertNoText("Вы пока не можете сменить тарифный план, т.к");
			button = browser.FindElementsByCssSelector(".connectfee");
			Assert.That(button.Count, Is.EqualTo(6), "На странице не должны отображаться кнопки для смены тарифа");
			Client.Plan.StoppageMonths = null;
			DbSession.Save(Client.Plan);
			DbSession.Flush();
			Open("Personal/Plans");
			AssertText("Вы пока не можете сменить тарифный план, т.к. подключились к нам менее 2-х месяцев назад.");
			button = browser.FindElementsByCssSelector(".connectfee");
			Assert.That(button.Count, Is.EqualTo(0), "На странице не должны отображаться кнопки для смены тарифа");
		}
	}
}