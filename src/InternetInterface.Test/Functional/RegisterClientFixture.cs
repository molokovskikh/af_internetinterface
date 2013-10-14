using System.Linq;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	class RegisterClientFixture : SeleniumFixture
	{
		[Test]
		public void RegisterClientTest()
		{
			var commutator = new NetworkSwitch("Тестовый коммутатор для регистрации клиента", session.Query<Zone>().First());
			var tariff = new Tariff("Тариф для тестирования", 111);
			Save(commutator, tariff);
			session.CreateSQLQuery("delete from internet.houses;").ExecuteUpdate();
			var testRegion1 = new RegionHouse { Name = "testRegionFirst" };
			session.Save(testRegion1);
			var house1 = new House("testStreetFirst", 1, testRegion1);
			session.Save(house1);
			Close();

			Open("Register/RegisterClient.rails");
			AssertText("Форма регистрации");
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

			Css("#Surname").SendKeys("TestSurname");
			Css("#Name").SendKeys("TestName");
			Css("#Patronymic").SendKeys("TestPatronymic");
			Css("#Apartment").SendKeys("5");
			Css("#Entrance").SendKeys("5");
			Css("#Floor").SendKeys("1");
			Css("#PhoneNumber").SendKeys("900-9009090");
			Css("#PassportSeries").SendKeys("1234");
			Css("#PassportNumber").SendKeys("123456");
			Css("#WhoGivePassport").SendKeys("TestWhoGivePassport");
			Css("#RegistrationAdress").SendKeys("TestRegistrationAdress");
			Css("#PassportDate").SendKeys("10.01.2002");
			Css("#client_ConnectSum").SendKeys("100");


			Css("#regionSelector").SelectByValue(testRegion1.Id.ToString());
			RunJavaScript("$('#regionSelector').change();");

			WaitForCss("#client_Tariff_Id");
			Css("#client_Tariff_Id").SelectByValue(tariff.Id.ToString());


			var checkbox = browser.FindElementByXPath("//input[@id='VisibleRegisteredInfo'][@type='checkbox']");
			if(!checkbox.Selected)
				checkbox.Click();

			Css("#RegisterClientButton").Click();
			WaitForText("прописанный по адресу:");
			AssertText("прописанный по адресу:");
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

		[Test, Ignore("Отключен функционал выбора коммутатора при регистрации")]
		public void Show_switch_comment()
		{
			var commutator = new NetworkSwitch("Тестовый коммутатор с комментарием", session.Query<Zone>().First()) {
				Comment = "Тестовый комментарий к коммутатору"
			};
			commutator.Name += " " + commutator.Id;
			session.Save(commutator);

			Open("Register/RegisterClient.rails");
			Css("#SelectSwitches").Select(commutator.Name);
			RunJavaScript("$('#SelectSwitches').change()");
			WaitForText(commutator.Comment);
			AssertText(commutator.Comment);
		}
	}
}
