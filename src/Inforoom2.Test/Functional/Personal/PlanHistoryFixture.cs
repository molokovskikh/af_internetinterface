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
	internal class PlanHistoryFixture : BaseFixture
	{
		public Client CurrentClient;
		public Plan OnceOnlyPlan;

		public void PlanChangerFixtureOn(int timeout)
		{
			CurrentClient = DbSession.Query<Client>().First(i => i.Comment == ClientCreateHelper.ClientMark.normalClient.GetDescription());
			//получаем разовый тариф
			OnceOnlyPlan = DbSession.Query<Plan>().First(s => s.IsOnceOnly);
			// присваиваем разовый тариф пользователю
			CurrentClient.PhysicalClient.Plan = OnceOnlyPlan;
			DbSession.SaveOrUpdate(CurrentClient);
			DbSession.Flush();
		}
		
		[Test(Description = "Проверка на ошибку, при повторном переходе на разовый тариф ")]
		public void PlanHistoryEntryChangeForOnceOnlyPlan()
		{
			PlanChangerFixtureOn(0);
			 LoginForClient(CurrentClient);
			Open("Personal/Plans");

			var planTo = DbSession.Query<Plan>().First(i => i.Name == "Оптимальный");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + planTo.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("input.connectfee"));
			button.Click();
			var confirm = browser.FindElementByCssSelector(".window .click.ok");
			confirm.Click(); 
			DbSession.Flush();
			DbSession.Refresh(CurrentClient);
			AssertText("Текущий тарифный план: " + planTo.Name); 

			 targetPlan = browser.FindElementByXPath("//td[contains(.,'" + OnceOnlyPlan.Name + "')]");
			 row = targetPlan.FindElement(By.XPath(".."));
			 button = row.FindElement(By.CssSelector("input.connectfee"));
			button.Click();
			 confirm = browser.FindElementByCssSelector(".window .click.ok");
			confirm.Click();

			AssertText("На данный тариф нельзя перейти вновь"); 
		}

		[Test(Description = "Проверка на уведомление, при смене тарифа с одноразового")]
		public void TransitionWithOnceOnlyPlan()
		{
			PlanChangerFixtureOn(0);
			LoginForClient(CurrentClient);
			Open("Personal/Plans");

			var planTo = DbSession.Query<Plan>().First(i => i.Name == "Оптимальный");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + planTo.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("input.connectfee"));
			button.Click();
			AssertText("Обратный переход на текущий тариф не возможен.");
			var confirm = browser.FindElementByCssSelector(".window .click.ok");
			confirm.Click();
		}
	}
}