using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.ConnectedHouses
{
	class ConnectedHousesFixture : ControlPanelBaseFixture
	{
		[Test, Description("Подключенные дома")]
		public void ConnectedHouseFixture()
		{
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
			Assert.That(DbSession.Query<ConnectedHouse>().Any(), Is.EqualTo(true), "Подключенных дома должны быть сгенерированы.");
			var region = DbSession.Query<Region>().FirstOrDefault(s => s.Streets.Count > 0);
			var streets = DbSession.Query<Street>().Where(s => s.Region.Id == region.Id && s.Houses.Count > 0).ToList();
			var houses = DbSession.Query<House>().Where(s => s.Street.Id == streets.First().Id).ToList();
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