using System.Linq;
using Inforoom2.Models;
using Inforoom2.Test.Functional.infrastructure;
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
			Assert.That(status, Is.EqualTo("Текущий"));
		}

		[Test(Description = "Смена тарифа")]
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
			Assert.That(status, Is.EqualTo("Текущий"));

			var client = Client;
			DbSession.Refresh(client);
			var appeal = DbSession.Query<Appeal>().FirstOrDefault();
			var writeoff = DbSession.Query<UserWriteOff>().FirstOrDefault();
			Assert.That(writeoff, Is.Not.Null);
			Assert.That(appeal, Is.Not.Null);
			Assert.That(appeal.Message, Is.StringContaining(planFrom.Name));
			Assert.That(appeal.Message, Is.StringContaining("(" + planFrom.Price + ")"));
			Assert.That(appeal.Message, Is.StringContaining(planTo.Name));
			Assert.That(appeal.Message, Is.StringContaining("(" + planTo.Price + ")"));
			Assert.That(appeal.Message, Is.StringContaining(writeoff.Sum.ToString()));
		}
	}
}