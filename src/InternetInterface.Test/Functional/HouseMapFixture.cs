using System;
using System.Collections.Generic;
using Common.Tools;
using InternetInterface.Models;
using NUnit.Framework;
using OpenQA.Selenium;
using Rhino.Mocks;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	internal class HouseMapFixture : SeleniumFixture
	{
		private RegionHouse _region;
		private House _house;

		[SetUp]
		public void SetUp()
		{
			var regionName = "Регион для теста" + DateTime.Now;
			_region = new RegionHouse {
				Name = regionName
			};
			session.Save(_region);
		}

		[Test(Description = "Тестирует изменение региона для дома")]
		public void EditHouse()
		{
			var region = new RegionHouse {
				Name = "Новый регион" + DateTime.Now
			};
			session.Save(region);
			var entrance = new Entrance();
			session.Save(entrance);
			var streetName = "улица" + DateTime.Now;
			_house = new House {
				Region = _region,
				Street = streetName,
				Case = "1",
				Number = 1,
				Entrances = new List<Entrance> {
					entrance
				}
			};
			session.Save(_house);
			session.Flush();
			Open(String.Format("HouseMap/ViewHouseInfo?House={0}", _house.Id));
			Click("Редактировать");
			Css("#house_Region_Id").SelectByValue(region.Id.ToString());
			Click("Сохранить");
			AssertText("Выберете дом:");

			session.Clear();
			var saved = session.Load<House>(_house.Id);
			Assert.That(saved.Region.Id, Is.EqualTo(region.Id));
		}

		[Test]
		public void FindHouseTest()
		{
			Open("HouseMap/FindHouse");
			Click("Найти");
			var rowsCount = browser.FindElements(By.XPath("//table[@id='find_result_table']/tbody/tr")).Count;
			Assert.Greater(rowsCount, 0);
			Css("#huise_link_0").Click();
		}

		[Test]
		public void EditHouseDetails()
		{
			Open(String.Format("HouseMap/ViewHouseInfo?House={0}", _house.Id));
			Css("#EnCount").SendKeys("10");
			Css("#EnCount").SendKeys(Keys.Return);
			Css("#ApCount").SendKeys("10");
			Click(Css("#editHouse"), "Назначить");
			WaitForText("Количество квартир: 10");
			AssertText("Количество квартир: 10");
			Click("Назначить проход");
			Css("#date_agent").SendKeys("01.09.2014");
			Click("Назначить в проход");
			WaitForText("Последний проход: 01.09.2014");
			AssertText("Последний проход: 01.09.2014");
		}
	}
}