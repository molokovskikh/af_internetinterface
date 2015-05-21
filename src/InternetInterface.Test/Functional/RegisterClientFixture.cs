using System;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture, Ignore("Страница 'RegisterClient' перенесена в новую админку")]
	public class RegisterClientFixture : SeleniumFixture
	{
		[Test]
		public void RegisterClientTest()
		{
			session.CreateSQLQuery("delete from houses;").ExecuteUpdate();

			var tariff = new Tariff("Тариф для тестирования", 111);
			session.Save(tariff);
			var region = new RegionHouse { Name = "testRegionFirst" };
			session.Save(region);
			var house = new House("testStreetFirst", 1, region);
			session.Save(house);

			Open("Register/RegisterClient");
			AssertText("Регистрация абонента");
			AssertText("Личная информация");
			AssertText("Фамилия");
			AssertText("Имя");
			AssertText("Отчество");
			AssertText("Город");
			AssertText("Адрес");
			AssertText("Паспортные данные");
			AssertText("Серия паспорта");
			AssertText("Номер паспорта");
			AssertText("Кем выдан");
			AssertText("Адрес регистрации");
			AssertText("Зарегистрировать");

			Css("#client_Surname").SendKeys("TestSurname" + Guid.NewGuid());
			Css("#client_Name").SendKeys("TestName");
			Css("#client_Patronymic").SendKeys("TestPatronymic");
			Css("#client_Apartment").SendKeys("5d");
			Css("#client_Additional").SendKeys("общежитие");
			Css("#client_Entrance").SendKeys("5");
			Css("#client_Floor").SendKeys("1");
			Css("#client_PhoneNumber").SendKeys("900-9009090");
			Css("#client_PassportSeries").SendKeys("1234");
			Css("#client_PassportNumber").SendKeys("123456");
			Css("#client_WhoGivePassport").SendKeys("TestWhoGivePassport");
			Css("#client_RegistrationAdress").SendKeys("TestRegistrationAdress");
			Css("#client_PassportDate").SendKeys("10.01.2002");
			Css("#client_ConnectSum").SendKeys("100");

			Css("#regionSelector").SelectByValue(region.Id.ToString());
			RunJavaScript("$('#regionSelector').change();");

			WaitForCss("#client_Tariff_Id");
			Css("#client_Tariff_Id").SelectByValue(tariff.Id.ToString());

			var checkbox = browser.FindElementByXPath("//input[@id='VisibleRegisteredInfo'][@type='checkbox']");
			if(!checkbox.Selected)
				checkbox.Click();

			Css("#RegisterClientButton").Click();
			WaitForText("прописанный по адресу:");
			AssertText("прописанный по адресу:");
			AssertText("5d");
			AssertText("адрес подключения:");
			AssertText("принимаю подключение к услугам доступа");

			AssertNoText("(4732) 606-000");
			AssertText("(473) 22-999-87");
		}

		[Test]
		public void City_house_selector_test()
		{
			session.CreateSQLQuery("delete from internet.houses;").ExecuteUpdate();
			var testRegion1 = new RegionHouse { Name = "testRegionFirst" };
			var testRegion2 = new RegionHouse { Name = "testRegionLast" };
			session.Save(testRegion1);
			session.Save(testRegion2);
			var house1 = new House("testStreetFirst", 1, testRegion1);
			var house2 = new House("testStreetlast", 2, testRegion2);
			session.Save(house1);
			session.Save(house2);
			Close();

			Open("Register/RegisterClient");
			Css("#regionSelector").SelectByValue(testRegion1.Id.ToString());
			RunJavaScript("$('#regionSelector').change()");
			WaitForCss("#houses_select");
			WaitForText("testStreetFirst");
			AssertText("testStreetFirst");
			AssertNoText("testStreetlast");

			Css("#regionSelector").SelectByValue(testRegion2.Id.ToString());
			RunJavaScript("$('#regionSelector').change()");
			WaitForText("testStreetlast");
			AssertText("testStreetlast");
			AssertNoText("testStreetFirst");
		}

		[Test]
		public void Warn_on_duplicate()
		{
			var client = ClientHelper.Client(session);
			var tariff = new Tariff("Тариф для тестирования", 111);
			session.Save(tariff);
			var region = new RegionHouse { Name = "testRegionFirst" };
			session.Save(region);
			var house = new House("testStreetFirst", 1, region);
			session.Save(house);

			Open("Register/RegisterClient");
			Css("#client_Surname").SendKeys(client.PhysicalClient.Surname);
			Css("#client_Name").SendKeys(client.PhysicalClient.Name);
			Css("#client_Patronymic").SendKeys(client.PhysicalClient.Patronymic);
			Css("#client_Apartment").SendKeys("5d");
			Css("#client_Entrance").SendKeys("5");
			Css("#client_Floor").SendKeys("1");
			Css("#client_PhoneNumber").SendKeys("900-9009090");
			Css("#client_ConnectSum").SendKeys("100");

			Css("#regionSelector").SelectByValue(region.Id.ToString());
			RunJavaScript("$('#regionSelector').change();");

			WaitForCss("#client_Tariff_Id");
			Css("#client_Tariff_Id").SelectByValue(tariff.Id.ToString());

			Click("Зарегистрировать");
			WaitAjax();
			AssertText("Клиент с таким именем уже существует");
			Click(Css(".ui-dialog"), "Продолжить");
			AssertText("Информация по клиенту");
		}

		[Test]
		public void Id_doc_validation()
		{
			Open("Register/RegisterClient");
			Css("#client_PassportSeries").SendKeys("a4512");
			Click("Зарегистрировать");
			Assert.That(GetValidationError("#client_PassportSeries"), Is.StringContaining("Неправильный формат серии паспорта (4 цифры)"));

			Css("#client_IdDocType").SelectByText("Иной документ");
			Click("Зарегистрировать");
			Assert.That(GetValidationError("#client_IdDocName"), Is.StringContaining("Это поле необходимо заполнить."));
			Assert.AreEqual("", GetValidationError("#client_PassportSeries"));
		}

		private string GetValidationError(string selector)
		{
			return Css(selector).FindElementByXPath("..").Text;
		}
	}
}
