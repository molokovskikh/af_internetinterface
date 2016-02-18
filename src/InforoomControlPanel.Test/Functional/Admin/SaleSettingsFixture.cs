using System.Linq;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Admin
{
	class SaleSettingsFixture : AdminFixture
	{
		[Test, Description("Добавление тарифного плана")]
		public void ChangeSaleSettings()
		{
			var PeriodCount = 331;
			var MinSale = 332;
			var MaxSale = 332;
			var SaleStep = 332;
			var DaysForRepair = 332;

			Open("Admin/SaleSettings");
			var inputObj = browser.FindElementByCssSelector("input[name='settings.PeriodCount']");
			Assert.That(inputObj.GetAttribute("value"), Is.Not.EqualTo(PeriodCount.ToString()), "Значение не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(PeriodCount.ToString());
             inputObj = browser.FindElementByCssSelector("input[name='settings.MinSale']");
			Assert.That(inputObj.GetAttribute("value"), Is.Not.EqualTo(MinSale.ToString()), "Значение не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(MinSale.ToString());
			 inputObj = browser.FindElementByCssSelector("input[name='settings.MaxSale']");
			Assert.That(inputObj.GetAttribute("value"), Is.Not.EqualTo(MaxSale.ToString()), "Значение не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(MaxSale.ToString());
			 inputObj = browser.FindElementByCssSelector("input[name='settings.SaleStep']");
			Assert.That(inputObj.GetAttribute("value"), Is.Not.EqualTo(SaleStep.ToString()), "Значение не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(SaleStep.ToString());
			 inputObj = browser.FindElementByCssSelector("input[name='settings.DaysForRepair']");
			Assert.That(inputObj.GetAttribute("value"), Is.Not.EqualTo(DaysForRepair.ToString()), "Значение не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(DaysForRepair.ToString());
			
			browser.FindElementByCssSelector(".btn-green").Click();

			var stSettings = DbSession.Query<SaleSettings>().FirstOrDefault();
			DbSession.Refresh(stSettings); 
			Assert.That(stSettings.PeriodCount, Is.EqualTo(PeriodCount), "Значение не совпадает.");
			Assert.That(stSettings.MinSale, Is.EqualTo(MinSale), "Значение не совпадает.");
			Assert.That(stSettings.MaxSale, Is.EqualTo(MaxSale), "Значение не совпадает.");
			Assert.That(stSettings.SaleStep, Is.EqualTo(SaleStep), "Значение не совпадает.");
			Assert.That(stSettings.DaysForRepair, Is.EqualTo(DaysForRepair), "Значение не совпадает.");
		}
	}
}
