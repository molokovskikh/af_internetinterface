using System;
using System.Linq;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class RegisterLawyerFixture : SeleniumFixture
	{
		[Test(Description = "Проверяет создание юр. лица с заказом и точкой подключения"), Ignore("Отключен функционал")]
		public void RegisterLegalPersonTest()
		{
			var commutator = new NetworkSwitch("Тестовый коммутатор для регистрации клиента", session.Query<Zone>().First());
			session.Save(commutator);
			Open("Register/RegisterLegalPerson.rails");
			var name = "Тестовый клиент" + DateTime.Now;
			browser.FindElementByName("LegalPerson_Name").Clear();
			browser.FindElementByName("LegalPerson_Name").SendKeys(name);
			browser.FindElementByName("LegalPerson_ShortName").Clear();
			browser.FindElementByName("LegalPerson_ShortName").SendKeys(name);
			browser.FindElementByName("order.Number").Clear();
			browser.FindElementByName("order.Number").SendKeys("99");
			Click("Добавить");
			browser.FindElementByName("order.OrderServices[1].Description").SendKeys("Тестовый заказ");
			browser.FindElementByName("order.OrderServices[1].Cost").SendKeys("1000");

			var checkbox = browser.FindElementByName("order.OrderServices[1].IsPeriodic");
			if(!checkbox.Selected)
				checkbox.Click();
			Css("#SelectSwitches").SelectByValue(commutator.Id.ToString());
			browser.FindElementByName("Port").SendKeys("1");
			Css("#RegisterLegalButton").Click();
			AssertText("Информация по клиенту " + name);
			var legalPerson = session.Query<LawyerPerson>().FirstOrDefault(l => l.Name == name);
			Assert.That(legalPerson, Is.Not.Null);
			Assert.That(legalPerson.client.Orders.First().OrderServices.Count, Is.EqualTo(1));
			var invoice = session.Query<Invoice>().Where(i => i.Client == legalPerson.client);
			Assert.That(invoice.Count(), Is.EqualTo(1));
			var contract = session.Query<Contract>().Where(c => c.Order == legalPerson.client.Orders.First());
			Assert.That(contract.Count(), Is.EqualTo(1));
		}

		[Test(Description = "Проверяет сохранение юр.лица без заказа")]
		public void RegisterLegalPersonWithoutOrderTest()
		{
			Open("Register/RegisterLegalPerson.rails");
			var name = "Тестовый клиент" + DateTime.Now;
			browser.FindElementById("LegalPerson_Name").SendKeys(name);
			browser.FindElementById("LegalPerson_ShortName").SendKeys(name);
			Css("#RegisterLegalButton").Click();
			AssertText("Информация по клиенту " + name);
		}

		[Test(Description = "Проверяет обязательность поля с датой начала для заказа")]
		public void RegisterLegalPersonWithoutBeginDateTest()
		{
			Open("Register/RegisterLegalPerson.rails");
			var name = "Тестовый клиент" + DateTime.Now;
			browser.FindElementById("LegalPerson_Name").SendKeys(name);
			browser.FindElementById("LegalPerson_ShortName").SendKeys(name);
			browser.FindElementById("order_BeginDate").Clear();
			Css("#RegisterLegalButton").Click();
			AssertText("Это поле необходимо заполнить");
		}
	}
}
