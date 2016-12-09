using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure.Helpers;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.ConnectedHouses
{
	class ConnectedHousesFixture : ControlPanelBaseFixture
	{
		/// <summary>
		/// Добавление улиц
		/// </summary>
		/// <param name="streetName"></param>
		/// <param name="anyExists"></param>
		/// <param name="errorDuble"></param>
		private void AddConnectedStreet(string streetName, bool anyExists= false, bool errorDuble = false)
		{
			var client =
				DbSession.Query<Client>()
					.FirstOrDefault(s => s.Comment == ClientCreateHelper.ClientMark.normalClient.GetDescription());
			var region = client.GetRegion();
			var street = client.Address.House.Street;
			//переход на страницу "Подключенных домов"
			Open("ConnectedStreets/");
			Assert.That(DbSession.Query<ConnectedStreet>().Any(), Is.EqualTo(anyExists), "Подключенных улиц быть не должно");
			Click("Добавить");
			//Регион
			Css("[id='RegionDropDown']").SelectByText(region.Name);
			//ожидание
			WaitForVisibleCss("#StreetDropDown option[value='" + street.Id + "']", 20);
			////Улица
			Css("[id='StreetDropDown']").SelectByText(street.Name);
			var inputObj = browser.FindElementByCssSelector("input[name='Name']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(""), "Name не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(streetName);
			Click("Добавить");
			if (errorDuble) {
				AssertText($"Подключенная улица '{streetName}' не может быть добавлена");
			}
		}

		[Test, Description("Подключенные дома")]
		public void ConnectedStreetsFixture()
		{
			//Добавление улиц
			var countOfNewStreets = 2;
			Assert.That(DbSession.Query<ConnectedStreet>().Count(), Is.EqualTo(0), "Подключенных улиц быть не должно");
			var streetNameFormat = "Новая улица №{0}";
			for (int i = 0; i < countOfNewStreets; i++) {
			AddConnectedStreet(String.Format(streetNameFormat,i), i != 0);
			}
			Assert.That(DbSession.Query<ConnectedStreet>().Count(), Is.EqualTo(countOfNewStreets),
				"Подключенных улиц меньше, чем должно быть");

			//Изменение первой добавленной улицы
			var newName = "NewStreet";
			var firstStreet = DbSession.Query<ConnectedStreet>().First();
			var newStreetId = DbSession.Query<Street>().First(s => s.Region.Id == firstStreet.Region.Id  && s.Id != firstStreet.AddressStreet.Id);
			Click("Редактировать");
			//ожидание
			WaitForVisibleCss("#StreetDropDown option[value='" + firstStreet.AddressStreet.Id + "']", 20);
			//Регион
			Css("[id='RegionDropDown']").SelectByText(newStreetId.Region.Name);
			//ожидание
			WaitForVisibleCss("#StreetDropDown option[value='" + newStreetId.Id + "']", 20);
			////Улица
			Css("[id='StreetDropDown']").SelectByText(newStreetId.Name);
			var inputObj = browser.FindElementByCssSelector("input[name='Name']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(firstStreet.Name), "Name не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(newName);
			Click("Редактировать");

			//Изменение видимости
			Click("Редактировать");
			//ожидание
			WaitForVisibleCss("#StreetDropDown option[value='" + newStreetId.Id + "']", 20);
			inputObj = browser.FindElementByCssSelector("input[name='Name']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(newName), "Name не совпадает.");
			inputObj = browser.FindElementByCssSelector("input[name='Disabled']");
			Assert.That(inputObj.GetAttribute("checked"), Is.Null, "Поле 'Скрыта' не совпадает.");
			inputObj.Click();
			Click("Редактировать");
			Assert.IsTrue(firstStreet.AddressStreet.Id != newStreetId.Id);
			Assert.IsTrue(firstStreet.Name != newName);
			DbSession.Refresh(firstStreet);
			Assert.IsTrue(firstStreet.AddressStreet.Id == newStreetId.Id);
			Assert.IsTrue(firstStreet.Name == newName);
			
			//Фильтрация
			var anotherStreet = DbSession.Query<ConnectedStreet>().First(s => s.Id != firstStreet.Id);
			var anotherRegion = DbSession.Query<Region>().First(s => s.Id != newStreetId.Region.Id);

			AssertText(firstStreet.Name);
			AssertText(anotherStreet.Name);
			//Регион
			Css("[name='mfilter.filter.Equal.Region.Name']").SelectByText(anotherRegion.Name);
			Click("Поиск");
			AssertNoText(firstStreet.Name);
			AssertNoText(anotherStreet.Name);
			//Регион
			Css("[name='mfilter.filter.Equal.Region.Name']").SelectByText(firstStreet.Region.Name);
			Click("Поиск");
			AssertText(firstStreet.Name);
			AssertText(anotherStreet.Name);
			//Улица скрыта
			Css("[name='mfilter.filter.Equal.Disabled']").SelectByText("Да");
			Click("Поиск");
			AssertText(firstStreet.Name);
			AssertNoText(anotherStreet.Name);
			Css("[name='mfilter.filter.Equal.Disabled']").SelectByText("Нет");
			Click("Поиск");
			AssertNoText(firstStreet.Name);
			AssertText(anotherStreet.Name);
		}

		[Test, Description("Подключенные дома")]
		public void ConnectedHouseFixture()
		{
			AddConnectedStreet("Новая улица");
			AddConnectedStreet("Новая улица", true, true);
			//тестовые значения
			var newHouseNumber = "111";
			var newHouseNumberFirst = "1";
			var newHouseNumberLast = "55";
			var newHouseNumberFirstLeft = 55;
			var newHouseNumberLastLeft = 69;
			var newHouseNumberFirstRight = 70;
			var newHouseNumberLastRight = 100;

			//переход на страницу "Подключенных домов"
			Open("ConnectedHouses/");
			//Генерация "записей по подключенным домам"
			Assert.That(DbSession.Query<ConnectedHouse>().Any(), Is.EqualTo(false), "Подключенных домов быть не должно");
			browser.FindElementByCssSelector("[data-target='#ModelForSynchronization']").Click();
			WaitForVisibleCss("#ModelForSynchronization .btn.btn-success");
			browser.FindElementByCssSelector("#ModelForSynchronization .btn.btn-success").Click();
			WaitForVisibleCss("[data-target='#ModelForSynchronization']");
			var region = DbSession.Query<Region>().FirstOrDefault(s => s.Streets.Count > 0);
			Assert.That(DbSession.Query<ConnectedHouse>().Any(), Is.EqualTo(true), "Подключенных дома должны быть сгенерированы.");
			var streets = DbSession.Query<ConnectedStreet>().Where(s => s.Region.Id == region.Id).ToList();
			var houses = DbSession.Query<House>().Where(s => s.Street.Id == streets.First().AddressStreet.Id).ToList();
			//Выбор тестового региона
			Css(".ConnectedHouses [name='regionId']").SelectByText(region.Name);
			browser.FindElementByCssSelector(".ConnectedHouses form .btn.btn-success").Click();
			WaitForVisibleCss(".ConnectedHouses form .btn.btn-success");
			AssertText(streets.First().Name);
			AssertText(houses.First().Number);
			//Добавление нового дома
			AssertNoText(newHouseNumber);
			browser.FindElementByCssSelector("[data-target='#ModelForConnectionHouseAdd']").Click();
			WaitForVisibleCss("#ModelForConnectionHouseAdd .btn.btn-success");
			Css("#ModelForConnectionHouseAdd [name='model.Street']").SelectByText(streets.First().Name);
			var inputObj = browser.FindElementByCssSelector("#ModelForConnectionHouseAdd input[name='model.House']");
			inputObj.Clear();
			inputObj.SendKeys(newHouseNumber);
			browser.FindElementByCssSelector("#ModelForConnectionHouseAdd .btn.btn-success").Click();
			WaitForVisibleCss(".ConnectedHouses form .btn.btn-success");
			AssertText(newHouseNumber);

			//Пакетное добавление домов - по обеим сторонам улицы
			AssertNoText(newHouseNumberLast);
			browser.FindElementByCssSelector("[data-target='#ModelForConnectionHouseBatchProcessing']").Click();
			WaitForVisibleCss("#ModelForConnectionHouseBatchProcessing .btn.btn-success");
			Css("#ModelForConnectionHouseBatchProcessing [name='streetId']").SelectByText(streets.First().Name);
			Css("#ModelForConnectionHouseBatchProcessing [name='side']").SelectByText("Обе");
			inputObj = browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing input[name='numberFirst']");
			inputObj.Clear();
			inputObj.SendKeys(newHouseNumberFirst);
			inputObj = browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing input[name='numberLast']");
			inputObj.Clear();
			inputObj.SendKeys(newHouseNumberLast);
			browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing .btn.btn-success").Click();
			WaitForVisibleCss(".ConnectedHouses form .btn.btn-success");
			AssertText(newHouseNumberLast);

			//Пакетное добавление домов - по левой стороне улицы
			AssertNoText(newHouseNumberLastLeft.ToString());
			AssertNoText((newHouseNumberLastLeft - 1).ToString());
			browser.FindElementByCssSelector("[data-target='#ModelForConnectionHouseBatchProcessing']").Click();
			WaitForVisibleCss("#ModelForConnectionHouseBatchProcessing .btn.btn-success");
			Css("#ModelForConnectionHouseBatchProcessing [name='streetId']").SelectByText(streets.First().Name);
			Css("#ModelForConnectionHouseBatchProcessing [name='side']").SelectByText("Левая");
			inputObj = browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing input[name='numberFirst']");
			inputObj.Clear();
			inputObj.SendKeys(newHouseNumberFirstLeft.ToString());
			inputObj = browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing input[name='numberLast']");
			inputObj.Clear();
			inputObj.SendKeys(newHouseNumberLastLeft.ToString());
			browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing .btn.btn-success").Click();
			WaitForVisibleCss(".ConnectedHouses form .btn.btn-success");
			AssertText(newHouseNumberLastLeft.ToString());
			AssertNoText((newHouseNumberLastLeft - 1).ToString());


			//Пакетное добавление домов - по правой стороне улицы
			AssertNoText(newHouseNumberLastRight.ToString());
			AssertNoText((newHouseNumberLastRight - 1).ToString());
			browser.FindElementByCssSelector("[data-target='#ModelForConnectionHouseBatchProcessing']").Click();
			WaitForVisibleCss("#ModelForConnectionHouseBatchProcessing .btn.btn-success");
			Css("#ModelForConnectionHouseBatchProcessing [name='streetId']").SelectByText(streets.First().Name);
			Css("#ModelForConnectionHouseBatchProcessing [name='side']").SelectByText("Правая");
			inputObj = browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing input[name='numberFirst']");
			inputObj.Clear();
			inputObj.SendKeys(newHouseNumberFirstRight.ToString());
			inputObj = browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing input[name='numberLast']");
			inputObj.Clear();
			inputObj.SendKeys(newHouseNumberLastRight.ToString());
			browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing .btn.btn-success").Click();
			WaitForVisibleCss(".ConnectedHouses form .btn.btn-success");
			AssertText(newHouseNumberLastRight.ToString());
			AssertNoText((newHouseNumberLastRight - 1).ToString());
			
			//Пакетное удаление домов - по обеим сторонам улицы
			AssertText(newHouseNumberFirstLeft.ToString());
			AssertText(newHouseNumberLastRight.ToString());
			browser.FindElementByCssSelector("[data-target='#ModelForConnectionHouseBatchProcessing']").Click();
			WaitForVisibleCss("#ModelForConnectionHouseBatchProcessing .btn.btn-success");
			Css("#ModelForConnectionHouseBatchProcessing [name='streetId']").SelectByText(streets.First().Name);
			Css("#ModelForConnectionHouseBatchProcessing [name='side']").SelectByText("Обе");
			Css("#ModelForConnectionHouseBatchProcessing [name='state']").SelectByText("Удалить");
			inputObj = browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing input[name='numberFirst']");
			inputObj.Clear();
			inputObj.SendKeys(newHouseNumberFirstLeft.ToString());
			inputObj = browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing input[name='numberLast']");
			inputObj.Clear();
			inputObj.SendKeys(newHouseNumberLastRight.ToString());
			browser.FindElementByCssSelector("#ModelForConnectionHouseBatchProcessing .btn.btn-success").Click();
			WaitForVisibleCss(".ConnectedHouses form .btn.btn-success");
			AssertNoText(newHouseNumberFirstLeft.ToString());
			AssertNoText((newHouseNumberFirstLeft + 1).ToString());
			AssertNoText(newHouseNumberLastRight.ToString());
			AssertNoText((newHouseNumberLastRight - 1).ToString());

			//Смена региона, проверка на отсутствие прежних значений (номер дома)
			AssertText(newHouseNumber.ToString());
			region = DbSession.Query<Region>().FirstOrDefault(s => s.Id != region.Id);
			Css(".ConnectedHouses [name='regionId']").SelectByText(region.Name);
			browser.FindElementByCssSelector(".ConnectedHouses form .btn.btn-success").Click();
			WaitForVisibleCss(".ConnectedHouses form .btn.btn-success");
			AssertNoText(newHouseNumber.ToString());
		}
	}
}