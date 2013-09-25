using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;

namespace InternetInterface.Test.Functional.Selenium
{
	[TestFixture, Ignore("Тесты имеют смысл только в случае наличия связи тарифы-регионы")]
	class TariffsFixture : SeleniumFixture
	{
		private RegionHouse _region;
		private IList<TariffChangeRule> _rules;

		[SetUp]
		public void FixtureSetup()
		{
			_region = new RegionHouse("TEST-REGION");
			_rules = session.Query<TariffChangeRule>().ToList();
			session.Save(_region);
		}

		[TearDown]
		public void FixtureTearDown()
		{
			session.Delete(_region);
		}

		[Test, Ignore]
		public void Filtering_rules_by_region()
		{
			/*Open("Tariffs/ChangeRules");
			var regions = browser.FindElementByCssSelector("#regionId").FindElements(By.CssSelector("option"));
			var testRegion = regions.First(r => r.Text != "Все");
			Assert.NotNull(testRegion);

			testRegion.Click();
			var rules = _rules.Where(r => _region.Tariffs.Contains(r.FromTariff) && _region.Tariffs.Contains(r.ToTariff)).ToList();
			var titles = browser.FindElementsByCssSelector(".wanted-item");

			Assert.IsTrue(rules.Count.Equals(titles.Count));*/
		}

		[Test, Ignore]
		public void Binding_tarrifs_to_region()
		{
			/*Open("Tariffs/ChangeRegions");
			var regions = browser.FindElementByCssSelector("#regionId").FindElements(By.CssSelector("option"));
			var testRegion = regions.Single(r => r.GetAttribute("value") == _region.Id.ToString());
			Assert.NotNull(testRegion);

			testRegion.Click();
			var checkBoxes = browser.FindElementsByCssSelector("input[type='checkbox']");
			foreach(var checkBox in checkBoxes) {
				checkBox.Click();
			}
			int tariffsCount = browser.FindElementsByCssSelector("input[type='checkbox']:checked").Count;
			ClickButton("Сохранить");

			session.Refresh(_region);
			Assert.IsTrue(_region.Tariffs.Count.Equals(tariffsCount));*/
		}
	}
}
