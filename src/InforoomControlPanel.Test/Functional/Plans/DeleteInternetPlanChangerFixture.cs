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
    class DeleteInternetPlanChangerFixture : PlanFixture
    {
        [Test, Description("Удаление правила смены тарифов для услуги Internet")]
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
            var planChangeDelete = DbSession.Query<PlanChangerData>().FirstOrDefault(p => p.Text == "Второе правило услуги Internet");
            var targetplanChange = browser.FindElementByXPath("//td[contains(.,'" + planChangeDelete.Text + "')]");
            var row = targetplanChange.FindElement(By.XPath(".."));
            var button = row.FindElement(By.CssSelector("a.btn-red"));
            button.Click();
            AssertText("Объект успешно удален!");
            var deletePlanChanger = DbSession.Query<PlanChangerData>().FirstOrDefault(p => p.Text == "Второе правило услуги Internet");
            Assert.That(deletePlanChanger, Is.Null, "Правило смены тарифов должно удалиться в базе данных");
        }
    }
}
