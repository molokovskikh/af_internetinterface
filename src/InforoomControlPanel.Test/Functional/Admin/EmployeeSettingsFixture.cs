using System.Linq;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Admin
{
	class EmployeeSettingsFixture : AdminFixture
	{
		[Test, Description("Добавление тарифного плана")]
		public void ChangePrice()
		{
			var newPriceA = 331;
			var newPriceB = 332;
			Open("Admin/EmployeeSettings");
			var inputObj = browser.FindElementByCssSelector("input[name='settingsA']");
			Assert.That(inputObj.GetAttribute("value"), Is.Not.EqualTo(newPriceA.ToString()), "Стоимость не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(newPriceA.ToString());
			inputObj = browser.FindElementByCssSelector("input[name='settingsB']");
			Assert.That(inputObj.GetAttribute("value"), Is.Not.EqualTo(newPriceB.ToString()), "Стоимость не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(newPriceB.ToString());
			browser.FindElementByCssSelector(".btn-green").Click();

			var stSettings = DbSession.Query<EmployeeTariff>().ToList(); 
			DbSession.Refresh(stSettings[0]);
			DbSession.Refresh(stSettings[1]);
			Assert.That(stSettings[0].Sum, Is.EqualTo(newPriceA), "Стоимость не совпадает.");
			Assert.That(stSettings[1].Sum, Is.EqualTo(newPriceB), "Стоимость не совпадает.");
		}
	}
}
