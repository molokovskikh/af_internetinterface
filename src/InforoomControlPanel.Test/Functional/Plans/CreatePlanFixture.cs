using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class CreatePlanFixture : PlanFixture
	{
		[Test, Description("Добавление тарифного плана")]
		public void PlanAdd()
		{
			Open("Plans/PlanIndex");
			browser.FindElementByCssSelector(".btn-green").Click();
			var planName = browser.FindElementByCssSelector("input[id=plan_Name]");
			var planPrice = browser.FindElementByCssSelector("input[id=plan_Price]");
			var planSpeed = browser.FindElementByCssSelector("select[id=PackageSpeedDropDown] option[value='1']");
			planName.SendKeys("Марс");
			planSpeed.Click();
			planPrice.Clear();
			planPrice.SendKeys("300");
			browser.FindElementByCssSelector(".btn-green").Click();
			AssertText("Тарифный план успешно добавлен");
			var plan = DbSession.Query<Plan>().First(p => p.Name == "Марс"); 
			var speed = DbSession.Query<PackageSpeed>().First(p => p.Speed == 40000000); 
			AssertText(plan.Name);
			Assert.That(plan.PackageSpeed.PackageId, Is.EqualTo(speed.PackageId), "Скорость у тарифа должна быть установлена корректно");  
		}
	}
}
