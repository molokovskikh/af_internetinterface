using System.Linq;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace Inforoom2.Test.Functional
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
			Assert.That(status,Is.EqualTo("Текущий"));
		}

		[Test(Description = "Смена тарифа")]
		public void ChangePlan()
		{
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'Оптимальный')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("input.connectfee"));
			button.Click();
			var confirm = browser.FindElementByCssSelector(".window .click.ok");
			confirm.Click();

			targetPlan = browser.FindElementByXPath("//td[contains(.,'Оптимальный')]");
			row = targetPlan.FindElement(By.XPath(".."));
			var status = row.FindElement(By.CssSelector(".paragraph")).Text;
			Assert.That(status,Is.EqualTo("Текущий"));
		}
	}
}