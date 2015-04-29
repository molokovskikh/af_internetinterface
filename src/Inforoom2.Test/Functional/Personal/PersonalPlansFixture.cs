using System.Linq;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure;
using Inforoom2.Test.Infrastructure.Helpers;
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
			Assert.That(writeoff.Comment, Is.StringContaining(planTo.Name), "В тексте описания денежного списания должно присутсвовать наименование тарифа,на который перешли");
			Assert.That(writeoff.Comment, Is.StringContaining(planFrom.Name), "В тексте описания денежного списания должно присутсвовать наименование тарифа,который сменили");
			Assert.That(appeal.Message, Is.StringContaining(planFrom.Name), "В тексте информационного сообщения о клиенте должно присутсвовать наименование тарифа,который сменили");
			Assert.That(appeal.Message, Is.StringContaining(planTo.Name), "В тексте информационного сообщения о клиенте должно присутсвовать наименование тарифа,на который перешли");
			Assert.That(appeal, Is.Not.Null, "У клиента должно быть информационно сообщение о смене тарифа");
			Assert.That(appeal.Message, Is.StringContaining("(" + planFrom.Price + ")"), "Информационное сообщение о клиенте должно содержать стоимость тарифа,который сменили");
			Assert.That(appeal.Message, Is.StringContaining(planTo.Name), "Информационное сообщение о клиенте должно содержать наименование тарифа,на который перешли");
			Assert.That(appeal.Message, Is.StringContaining("(" + planTo.Price + ")"), "Информационное сообщение о клиенте должно содержать стоимость тарифа,на который перешли");
			Assert.That(appeal.Message, Is.StringContaining(writeoff.Sum.ToString()), "Информационное сообщение о клиенте должно содержать стоимость смены тарифа");
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

		[Test(Description = "Новому клиенту не доступна смена тарифа первые два месяца.")]
		public void PlanRecentClient()
		{
			var Client = DbSession.Query<Client>().First(i => i.Comment == ClientCreateHelper.ClientMark.recentClient.GetDescription());
			LoginForClient(Client);
			Open("Personal/Plans");
			AssertText("Вы пока не можете сменить тарифный план, т.к. подключились к нам менее 2-х месяцев назад.");
			var button = browser.FindElementsByCssSelector(".connectfee");
			Assert.That(button.Count, Is.EqualTo(0), "На странице не должны отображаться кнопки для смены тарифа");
		}


	}
}