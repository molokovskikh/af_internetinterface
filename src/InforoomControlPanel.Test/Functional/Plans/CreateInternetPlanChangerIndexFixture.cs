using System.Linq;
using Inforoom2.Models;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Plans
{
	class CreateInternetPlanChangerFixture : PlanFixture
	{
		[Test, Description("Успешное добавление правила смены тарифов для услуги Internet")]
		public void successfulCreateInternetPlanChanger()
		{
			Open("Plans/InternetPlanChangerIndex");
			browser.FindElementByCssSelector("i.entypo-plus").Click();
            Css(".targetPlan").SelectByText("50 на 50 (245 руб.)");
            Css(".cheapPlan").SelectByText("Старт (245 руб.)");
            Css(".fastPlan").SelectByText("Народный (300 руб.)");
            browser.FindElementByCssSelector("input[id=planChanger_Timeout]").SendKeys("30");
            browser.FindElementByCssSelector("textarea[id=planChanger_Text]").SendKeys("<p>Услуга Internet</p>");
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Объект успешно добавлен!");
            var createPlanChanger =  DbSession.Query<PlanChangerData>().FirstOrDefault(p => p.Text == "<p>Услуга Internet</p>");
            Assert.That(createPlanChanger, Is.Not.Null, "Правило смены тарифов должно сохраниться в базе данных");
        }

        [Test, Description("Не успешное добавление правила смены тарифов для услуги Internet")]
        public void unsuccessfulCreateInternetPlanChanger()
        {
            var planChangerCount = DbSession.Query<PlanChangerData>().Count();
            Open("Plans/InternetPlanChangerIndex");
            browser.FindElementByCssSelector("i.entypo-plus").Click();
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("выберите тариф");
            DbSession.Flush();
            var planChangerCountAfterTest = DbSession.Query<PlanChangerData>().Count();
            Assert.That(planChangerCountAfterTest, Is.EqualTo(planChangerCount), "Правило смены тарифов не должно сохраниться в базе данных");
        }
    }
}