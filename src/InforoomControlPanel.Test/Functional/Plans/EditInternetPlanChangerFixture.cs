using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace InforoomControlPanel.Test.Functional.Plans
{
    class EditInternetPlanChangerFixture : PlanFixture
    {
        [Test, Description("Изменение правила смены тарифов для услуги Internet")]
        public void DeleteInternetPlanChanger()
        {
            var cheapPlan = DbSession.Query<Plan>().FirstOrDefault(p => p.Name == "50 на 50");
            var fastPlan = DbSession.Query<Plan>().FirstOrDefault(p => p.Name == "Старт");
            var targetPlan = DbSession.Query<Plan>().FirstOrDefault(p => p.Name == "Народный");
            var planChange = new PlanChangerData();
            planChange.CheapPlan = cheapPlan;
            planChange.FastPlan = fastPlan;
            planChange.TargetPlan = targetPlan;
            planChange.Timeout = 30;
            planChange.Text = "Второе правило услуги Internet";
            DbSession.Save(planChange);

            Open("Plans/InternetPlanChangerIndex");
            var planChangeEdit = DbSession.Query<PlanChangerData>().FirstOrDefault(p => p.Text == "Второе правило услуги Internet");
            var targetplanChange = browser.FindElementByXPath("//td[contains(.,'" + planChangeEdit.Text + "')]");
            var row = targetplanChange.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector(".entypo-cancel-circled"));
            button.Click();
            Css(".targetPlan").SelectByText("50 на 50 (245 руб.)");
            Css(".cheapPlan").SelectByText("Старт (245 руб.)");
            Css(".fastPlan").SelectByText("Народный (300 руб.)");
            browser.FindElementByCssSelector("input[id=planChanger_Timeout]").Clear();
            browser.FindElementByCssSelector("input[id=planChanger_Timeout]").SendKeys("20");
            browser.FindElementByCssSelector("textarea[id=planChanger_Text]").Clear();
            browser.FindElementByCssSelector("textarea[id=planChanger_Text]").SendKeys("<p>Первое правило услуги Internet");
            browser.FindElementByCssSelector(".btn-green").Click();
            AssertText("Объект успешно изменен!");
            DbSession.Refresh(planChangeEdit);
            Assert.That(planChangeEdit.CheapPlan, Is.EqualTo(fastPlan), "Изменения в правиле смены тарифов для услуги Internet должны сохраниться корректно");
            Assert.That(planChangeEdit.FastPlan, Is.EqualTo(targetPlan), "Изменения в правиле смены тарифов для услуги Internet должны сохраниться корректно");
            Assert.That(planChangeEdit.TargetPlan, Is.EqualTo(cheapPlan), "Изменения в правиле смены тарифов для услуги Internet должны сохраниться корректно");
            Assert.That(planChangeEdit.Timeout, Is.EqualTo(20), "Изменения в правиле смены тарифов для услуги Internet должны сохраниться корректно");
            Assert.That(planChangeEdit.Text, Is.EqualTo("<p>Первое правило услуги Internet"), "Изменения в правиле смены тарифов для услуги Internet должны сохраниться корректно");
        }
    }
}
