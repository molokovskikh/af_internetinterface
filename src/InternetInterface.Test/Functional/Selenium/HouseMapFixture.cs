using System;
using System.Collections.Generic;
using System.Linq;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;
using OpenQA.Selenium;

namespace InternetInterface.Test.Functional.Selenium
{
	[TestFixture]
	internal class HouseMapFixture : SeleniumFixture
	{
		private RegionHouse _region;

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
			var house = new House {
				Region = _region,
				Street = streetName,
				Case = "1",
				Number = 1,
				Entrances = new List<Entrance> {
					entrance
				}
			};
			session.Save(house);
			session.Flush();
			Open(String.Format("HouseMap/ViewHouseInfo?House={0}", house.Id));
			Click("Редактировать");
			Css("#house_Region_Id").SelectByValue(region.Id.ToString());
			Click("Сохранить");
			AssertText("Выберете дом:");

			session.Clear();
			var saved = session.Load<House>(house.Id);
			Assert.That(saved.Region.Id, Is.EqualTo(region.Id));
		}

		[Test]
		public void FindHouseTest()
		{
			Open("HouseMap/FindHouse.rails");
			Click("Найти");
			var rowsCount = browser.FindElements(By.XPath("//table[@id='find_result_table']/tbody/tr")).Count;
			Assert.Greater(rowsCount, 0);
			Css("#huise_link_0").Click();
		}
	}
}
