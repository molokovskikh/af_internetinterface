using System;
using System.Linq;
using Common.Tools.Calendar;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium.Support.UI;

namespace InforoomControlPanel.Test.Functional.ClientActions
{
	internal class AgentListFixture : ClientActionsFixture
	{
		[Test, Description("Добавление агента")]
		public void AgentAdd()
		{
			Open("Client/AgentList");
			var agentName = browser.FindElementByCssSelector("input[id=agent_Name]");
			agentName.SendKeys("Тестов Тест");
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("Агент успешно добавлен");
			AssertText("Тестов Тест");
			var agent = DbSession.Query<Agent>().FirstOrDefault(p => p.Name == "Тестов Тест");
			Assert.That(agent.Name, Does.Contain("Тестов Тест"), "Агент должен сохраниться и в базе данных");
		}
	}
}