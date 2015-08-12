using System.Linq;
using System.Web.WebPages;
using Common.Tools;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Rhino.Mocks;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class EditPlanFixture : PlanFixture
	{
		[Test, Description("Изменение данных тарифного плана")]
		public void EditPlan()
		{
			Open("Plans/PlanIndex");
			var plan = DbSession.Query<Plan>().First(p => p.Name == "Максимальный");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + plan.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			var planName = browser.FindElementByCssSelector("input[id=plan_Name]");
			var planSpeed = browser.FindElementByCssSelector("select[id=PackageSpeedDropDown] option[value='1']");
			var planPrice = browser.FindElementByCssSelector("input[id=plan_Price]");
			var planFeatures = browser.FindElementByCssSelector("textarea[id=plan_Features]");
			var planDescription = browser.FindElementByCssSelector("textarea[id=plan_Description]");
			var disabled = browser.FindElementByCssSelector("input[id=Disabled]").GetAttribute("checked")!=null;
			var availableForNewClients = browser.FindElementByCssSelector("input[id=AvailableForNewClients]").GetAttribute("checked") != null;
			var availableForOldClients = browser.FindElementByCssSelector("input[id=AvailableForOldClients]").GetAttribute("checked") != null;
			planName.Clear();
			planName.SendKeys("Максимальный Измененный");
			planSpeed.Click();
			planPrice.Clear();
			planPrice.SendKeys("300");
			browser.FindElementByCssSelector("input[id=withBonus]").Click();
			browser.FindElementByCssSelector("input[id=Disabled]").Click();
			browser.FindElementByCssSelector("input[id=AvailableForNewClients]").Click();
			browser.FindElementByCssSelector("input[id=AvailableForOldClients]").Click();
			planFeatures.SendKeys("Тест");
			planDescription.SendKeys("Для теста изменен");
			browser.FindElementByCssSelector(".btn-green.save").Click();
			AssertText("Тарифный план успешно отредактирован");
			DbSession.Refresh(plan);
			var speed = DbSession.Query<PackageSpeed>().First(p => p.Speed == 40000000);
			Assert.That(plan.Name, Is.StringContaining("Максимальный Измененный"), "Изменение наименования тарифа должно сохраниться и в базе данных");
			//У тарифа должен сохраниться правильный Id-скорости
			Assert.That(plan.PackageSpeed.PackageId, Is.EqualTo(speed.PackageId), "Скорость у тарифа должна быть установлена корректно");
			Assert.That(plan.Price, Is.EqualTo(300), "Изменение цены тарифа должно сохраниться и в базе данных");
			Assert.That(plan.IgnoreDiscount, Is.True, "Изменение отметки о скидке тарифа должно сохраниться и в базе данных");
			Assert.That(plan.Disabled, Is.EqualTo(!disabled), "Изменение отметки о скрытости тарифа должно сохраниться и в базе данных");
			Assert.That(plan.AvailableForNewClients, Is.EqualTo(!availableForNewClients), "Изменение отметки об опубликованности для новых клиентов тарифа должно сохраниться и в базе данных");
			Assert.That(plan.AvailableForOldClients, Is.EqualTo(!availableForOldClients), "Изменение отметки об опубликованности для старых клиентов тарифа должно сохраниться и в базе данных");
			Assert.That(plan.Features, Is.StringContaining("Тест"), "Изменение в заголовке тарифа должно сохраниться и в базе данных");
			Assert.That(plan.Description, Is.StringContaining("Для теста изменен"), "Изменение в описании тарифа должно сохраниться и в базе данных");			
		}

		[Test, Description("Удаление стоимости перехода на другой тариф")]
		public void DeletePlanPrice()
		{
			Open("Plans/PlanIndex");
			var plan = DbSession.Query<Plan>().First(p => p.Name == "Оптимальный");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + plan.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			var planEdit = DbSession.Query<Plan>().First(p => p.Name == "Старт");
			var targetPlanEdit = browser.FindElementByXPath("//div[@class='col-sm-5'][contains(.,'" + planEdit.Name + "')]");
			var rowPlanPrice = targetPlanEdit.FindElement(By.XPath(".."));
			var buttonPlanTransfers = rowPlanPrice.FindElement(By.CssSelector(".entypo-cancel-circled"));
			buttonPlanTransfers.Click();
			AssertText("Стоимость перехода успешно удалена");
			browser.FindElementByCssSelector(".btn-green.save").Click();
			DbSession.Refresh(plan);
			var PlanTransfers = plan.PlanTransfers.ToList().FirstOrDefault(i => i.PlanTo.Name == "Старт");
			Assert.That(PlanTransfers, Is.Null, "Стоимость перехода на другой ТП должна удалиться и в базе данных");
		}

		[Test, Description("Добавление стоимости перехода тарифа на другой тариф ")]
		public void AddPlanTransfers()
		{
			var planToAddPrice = DbSession.Query<Plan>().First(p => p.Name == "Оптимальный");
			var plan = new Plan();
			plan.Price = 500;
			plan.Name = "Венера";
			plan.Disabled = false;
			plan.Hidden = false;
			plan.PackageSpeed = DbSession.Get<PackageSpeed>(6);
			plan.IsServicePlan = false;
			DbSession.Save(plan);
			Open("Plans/PlanIndex");
			var planEdit = DbSession.Query<Plan>().First(p => p.Name == "Венера");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + planEdit.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			Css("#PlanDropDown").SelectByText(planToAddPrice.Name + " (" + planToAddPrice.Price + " руб.)");
			browser.FindElementByCssSelector("input[id=PlanTransfer_Price]").Clear();
			browser.FindElementByCssSelector("input[id=PlanTransfer_Price]").SendKeys("300");
			browser.FindElementByCssSelector(".btn-green.addPrice").Click();
			AssertText("Стоимость перехода успешно отредактирован");
			DbSession.Refresh(plan);
			var planTransfers = plan.PlanTransfers.ToList().FirstOrDefault(i => i.PlanTo.Name == "Оптимальный");
			Assert.That(planTransfers.PlanTo.Name, Is.StringContaining("Оптимальный"), "Переход на другой ТП должн добавиться и в базе данных");
			Assert.That(planTransfers.Price, Is.EqualTo(300), "Стоимость перехода на другой ТП должна добавиться и в базе данных");
		}

		[Test, Description("Добавление региона для которого будет доступен тариф")]
		public void AddPlanRegion()
		{
			Open("Plans/PlanIndex");
			var plan = DbSession.Query<Plan>().First(p => p.Name == "Оптимальный");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + plan.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			Css("#RegionDropDown").SelectByText("Белгород");
			browser.FindElementByCssSelector(".btn-green.addRegion").Click();
			AssertText("Регион успешно добавлен");
			browser.FindElementByCssSelector(".btn-green.save").Click();
			DbSession.Refresh(plan);
			var addPlanRegion = plan.RegionPlans.ToList().FirstOrDefault(i => i.Region.Name == "Белгород");
			Assert.That(addPlanRegion.Region.Name, Is.StringContaining("Белгород"), "У тарифа регион для которого он будет доступен должен сохраниться и в базе данных");
		}

		[Test, Description("Удаление региона для которого будет доступен тариф")]
		public void DeletePlanRegion()
		{
			Open("Plans/PlanIndex");
			var plan = DbSession.Query<Plan>().First(p => p.Name == "Тариф с указанным регионом");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + plan.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			var planRegion = DbSession.Query<Region>().First(p => p.Name == "Борисоглебск");
			var targetPlanPrice = browser.FindElementByXPath("//div[@class='col-sm-5 region'][contains(.,'" + planRegion.Name + "')]");
			var rowPlanRegion = targetPlanPrice.FindElement(By.XPath(".."));
			var buttonPlanPrice = rowPlanRegion.FindElement(By.CssSelector(".entypo-cancel-circled"));
			buttonPlanPrice.Click();
			AssertText("Регион успешно удален");
			browser.FindElementByCssSelector(".btn-green.save").Click();
			DbSession.Refresh(plan);
			var deletePlanRegion = plan.RegionPlans.ToList().FirstOrDefault(i => i.Region.Name == "Борисоглебск");
			Assert.That(deletePlanRegion, Is.Null, "Регион у тарифного плана должен удалиться и в базе данных");
		}

		[Test, Description("Добавление группы каналов в тарифный план")]
		public void AddPlanTVChannelGroup()
		{
			Open("Plans/PlanIndex");
			var plan = DbSession.Query<Plan>().First(p => p.Name == "Оптимальный");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + plan.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			Css("#TvChannelGroupDropDown").SelectByText("Спорт");
			browser.FindElementByCssSelector(".btn-green.addTVChannel").Click();
			AssertText("Объект успешно прикреплен!");
			browser.FindElementByCssSelector(".btn-green.save").Click();
			DbSession.Refresh(plan);
			var addPlanTvChannelGroup = plan.TvChannelGroups.ToList().FirstOrDefault(i => i.Name == "Спорт");
			Assert.That(addPlanTvChannelGroup.Name, Is.StringContaining("Спорт"), "Группа каналов должна добавиться тарифному плану и в базе данных");
		}

		[Test, Description("Удаление группы каналов из тарифного плана")]
		public void DeletePlanTVChannelGroup()
		{
			Open("Plans/PlanIndex");
			var plan = DbSession.Query<Plan>().First(p => p.Name == "Популярный");
			var targetPlan = browser.FindElementByXPath("//td[contains(.,'" + plan.Name + "')]");
			var row = targetPlan.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-success"));
			button.Click();
			var planTvChannelGroup = DbSession.Query<TvChannelGroup>().First(p => p.Name == "Основная");
			var targetPlanTvChannelGroups = browser.FindElementByXPath("//div[@class='col-sm-5 tvChannelGroups'][contains(.,'" + planTvChannelGroup.Name + "')]");
			var rowPlanTvChannelGroup = targetPlanTvChannelGroups.FindElement(By.XPath(".."));
			var buttonPlanTvChannelGroup = rowPlanTvChannelGroup.FindElement(By.CssSelector(".entypo-cancel-circled"));
			buttonPlanTvChannelGroup.Click();
			AssertText("Тарифный план успешно отредактирован");
			browser.FindElementByCssSelector(".btn-green.save").Click();
			DbSession.Refresh(plan);
			var deletePlanTvChannelGroup = plan.RegionPlans.ToList().FirstOrDefault(i => i.Region.Name == "Борисоглебск");
			Assert.That(deletePlanTvChannelGroup, Is.Null, "Группа каналов у тарифного плана должена удалиться и в базе данных");
		}
	}
}