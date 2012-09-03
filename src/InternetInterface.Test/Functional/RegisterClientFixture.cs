using System;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using WatiN.Core;
using WatiN.Core.Native.Windows;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class RegisterClientFixture : global::Test.Support.Web.WatinFixture2
	{
		[Test]
		public void RegisterClientTest()
		{
			var commutator = new NetworkSwitch("Тестовый коммутатор для регистрации клиента", session.Query<Zone>().First());
			session.Save(commutator);

			Open("Register/RegisterClient.rails");
			Assert.That(browser.Text, Is.StringContaining("Форма регистрации"));
			Assert.That(browser.Text, Is.StringContaining("Личная информация"));
			Assert.That(browser.Text, Is.StringContaining("Фамилия"));
			Assert.That(browser.Text, Is.StringContaining("Имя"));
			Assert.That(browser.Text, Is.StringContaining("Отчество"));
			Assert.That(browser.Text, Is.StringContaining("Город"));
			Assert.That(browser.Text, Is.StringContaining("Адрес"));
			Assert.That(browser.Text, Is.StringContaining("Паспортные данные"));
			Assert.That(browser.Text, Is.StringContaining("Серия паспорта"));
			Assert.That(browser.Text, Is.StringContaining("Номер паспорта"));
			Assert.That(browser.Text, Is.StringContaining("Кем выдан"));
			Assert.That(browser.Text, Is.StringContaining("Адрес регистрации"));
			Assert.That(browser.Text, Is.StringContaining("Тариф"));
			Assert.That(browser.Text, Is.StringContaining("Внести сумму"));
			Assert.That(browser.Text, Is.StringContaining("Зарегистрировать"));
			Css("#Surname").AppendText("TestSurname");
			Css("#Name").AppendText("TestName");
			Css("#Patronymic").AppendText("TestPatronymic");
			Css("#City").AppendText("TestCity");
			Css("#Apartment").AppendText("5");
			Css("#Entrance").AppendText("5");
			Css("#Floor").AppendText("1");
			Css("#PhoneNumber").AppendText("900-9009090");
			Css("#PassportSeries").AppendText("1234");
			Css("#PassportNumber").AppendText("123456");
			Css("#WhoGivePassport").AppendText("TestWhoGivePassport");
			Css("#RegistrationAdress").AppendText("TestRegistrationAdress");
			Css("#PassportDate").AppendText("10.01.2002");
			Css("#client_ConnectSum").AppendText("100");
			Css("#client_Tariff_Id").Select("Тариф для тестирования");

			browser.SelectList("SelectSwitches").Select("Тестовый коммутатор для регистрации клиента");
			browser.Eval(String.Format("$('#SelectSwitches').change()"));
			var occupiedPorts = browser.Elements.Count(e => e.ClassName == "access_port");
			Assert.That(occupiedPorts, Is.EqualTo(0));

			browser.CheckBox("VisibleRegisteredInfo").Checked = true;
			browser.Button(Find.ById("RegisterClientButton")).Click();

			browser.WaitUntilContainsText("прописанный по адресу:", 2);
			Assert.That(browser.Text, Is.StringContaining("прописанный по адресу:"));
			Assert.That(browser.Text, Is.StringContaining("адрес подключения:"));
			Assert.That(browser.Text, Is.StringContaining("принимаю подключение к услугам доступа"));

			Assert.That(browser.Text, Is.Not.Contains("(4732) 606-000"));
			AssertText("(473) 22-999-87");
		}

		[Test]
		public void Show_switch_comment()
		{
			var commutator = new NetworkSwitch("Тестовый коммутатор с комментарием", session.Query<Zone>().First()) {
				Comment = "Тестовый комментарий к коммутатору"
			};
			session.Save(commutator);
			session.Flush();
			commutator.Name += " " + commutator.Id;
			session.Save(commutator);

			Open("Register/RegisterClient.rails");
			Css("#SelectSwitches").Select(commutator.Name);
			browser.Eval(String.Format("$('#SelectSwitches').change()"));
			browser.WaitUntilContainsText(commutator.Comment, 2);
			AssertText(commutator.Comment);
		}
	}
}