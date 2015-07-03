using System;
using System.Linq;
using Common.Tools.Calendar;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace InforoomControlPanel.Test.Functional.ClientActions
{
	internal class agentStatusChangeFixture : ClientActionsFixture
	{
		[Test, Description("Активация агента")]
		public void agentStatusChangeActiveTrue()
		{
			Open("Client/AgentList");
			var agent = DbSession.Query<Agent>().FirstOrDefault(p => p.Name == "Егоров Павел");
			var targetAgent = browser.FindElementByXPath("//td[contains(.,'" + agent.Name + "')]");
			var row = targetAgent.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn.btn-green.btn-sm"));
			button.Click();
			DbSession.Refresh(agent);
			Assert.That(agent.Active, Is.True, "Агент должен активироваться и в базе данных");			
		}

		[Test, Description("Дезактивация агента")]
		public void agentStatusChangeActiveFalse()
		{
			Open("Client/AgentList");
			var agent = DbSession.Query<Agent>().FirstOrDefault(p => p.Name == "Павлов Дмитрий");
			var targetAgent = browser.FindElementByXPath("//td[contains(.,'" + agent.Name + "')]");
			var row = targetAgent.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn.btn-red.btn-sm"));
			button.Click();
			DbSession.Refresh(agent);
			Assert.That(agent.Active, Is.False, "Агент должен дезктивироваться и в базе данных");
		}
	}
}