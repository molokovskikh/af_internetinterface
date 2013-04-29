using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Web;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class RegisterLawyerFixture : WatinFixture2
	{
		[Test(Description = "Проверяет создание юр. лица с заказом и точкой подключения"), Ignore("Отключен функционал")]
		public void RegisterLegalPersonTest()
		{
			var commutator = new NetworkSwitch("Тестовый коммутатор для регистрации клиента", session.Query<Zone>().First());
			session.Save(commutator);
			Open("Register/RegisterLegalPerson.rails");
			var name = "Тестовый клиент" + DateTime.Now;
			browser.TextField("LegalPerson_Name").AppendText(name);
			browser.TextField("LegalPerson_ShortName").AppendText(name);
			browser.TextField(Find.ByName("order.Number")).TypeText("99");
			Click("Добавить");
			browser.TextField(Find.ByName("order.OrderServices[1].Description")).AppendText("Тестовый заказ");
			browser.TextField(Find.ByName("order.OrderServices[1].Cost")).AppendText("1000");
			browser.CheckBox(Find.ByName("order.OrderServices[1].IsPeriodic")).Checked = true;
			browser.SelectList("SelectSwitches").SelectByValue(commutator.Id.ToString());
			browser.TextField("Port").AppendText("1");
			browser.Button("RegisterLegalButton").Click();
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
			browser.TextField("LegalPerson_Name").AppendText(name);
			browser.TextField("LegalPerson_ShortName").AppendText(name);
			browser.ShowWindow(NativeMethods.WindowShowStyle.Maximize);
			browser.Button("RegisterLegalButton").Click();
			AssertText("Информация по клиенту " + name);
		}

		[Test(Description = "Проверяет обязательность поля с датой начала для заказа")]
		public void RegisterLegalPersonWithoutBeginDateTest()
		{
			Open("Register/RegisterLegalPerson.rails");
			var name = "Тестовый клиент" + DateTime.Now;
			browser.TextField("LegalPerson_Name").AppendText(name);
			browser.TextField("LegalPerson_ShortName").AppendText(name);
			browser.TextField("order_BeginDate").Value = "";
			browser.Button("RegisterLegalButton").Click();
			AssertText("Это поле необходимо заполнить");
		}
	}
}
