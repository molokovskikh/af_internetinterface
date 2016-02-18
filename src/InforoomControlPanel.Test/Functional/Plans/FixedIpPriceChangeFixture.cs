using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class FixedIpPriceChangeFixture : PlanFixture
	{
		[Test, Description("Добавление тарифного плана")]
		public void ChangePrice()
		{
			var newPrice = 333;
			Open("Plans/FixedIpPrice");
			var inputObj = browser.FindElementByCssSelector("input[name='price']");
			Assert.That(inputObj.GetAttribute("value"), Is.Not.EqualTo(newPrice.ToString()), "Стоимость не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(newPrice.ToString());
			browser.FindElementByCssSelector(".btn-green").Click();
			var priceItem = DbSession.Query<Service>().FirstOrDefault(s => s.Id == Service.GetIdByType(typeof(FixedIp)));
			Assert.That(priceItem.Price, Is.EqualTo(newPrice), "Стоимость не совпадает.");
		}
	}
}
