using System.Linq;
using System.Web;
using Inforoom2.Models;
using Inforoom2.Test.Functional.Personal;
using Inforoom2.Test.Infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace Inforoom2.Test.Functional.Home
{
	public class CallMeBackFixture : PersonalFixture
	{
		[Test, Description("Проверка возможности запросить обратный звонок, так же проверяется forwarding")]
		public void CallMeBackTicket()
		{
			Open("Faq");
			browser.FindElementByCssSelector(".call").Click();
			var name = browser.FindElementByCssSelector("input[id=callMeBackTicket_Name]");
			var phone = browser.FindElementByCssSelector("input[id=callMeBackTicket_PhoneNumber]");
			var comment = browser.FindElementByName("callMeBackTicket.Text");
			name.SendKeys("Иван Петров");
			phone.SendKeys("8556478970");
			comment.SendKeys("my question");
			browser.FindElementByCssSelector(".contacting").Click();
			var callMeBackTicket = DbSession.Query<CallMeBackTicket>().FirstOrDefault(c => c.PhoneNumber == "8556478970"); 
			Assert.NotNull(callMeBackTicket);
			var count = DbSession.Query<CallMeBackTicket>().ToList().Count;
			Assert.That(count, Is.EqualTo(1), "Создается только одна заявка за раз");
			AssertText("Задать вопрос:");
		}

		[Test, Description("Проверка смены города")]
		public void ChangeCity()
		{
			Open("");
			browser.FindElementByCssSelector(".arrow").Click();
			var oldcity = browser.FindElementByCssSelector(".city .name").Text;
			var clickCity = browser.FindElementByCssSelector(".cities a");
			var clickedText = clickCity.Text;

			clickCity.Click();

			Assert.That(oldcity, Is.Not.StringContaining(clickedText), "Выбранный город не должен быть равен изначальному");
			var name = browser.FindElementByCssSelector(".city .name").Text;
			Assert.That(name, Does.Contain(clickedText), "Изначальный город должен поменяться");
		}
		
		[Test(Description = "Выбор акционного тарифа")]
		public void PromotionalPlan()
		{
			Open("");
			browser.FindElementByCssSelector(".arrow").Click();
			var clickCity = browser.FindElementByLinkText("Борисоглебск (частный сектор)");
			var clickedText = clickCity.Text;
			clickCity.Click();
			browser.FindElementByCssSelector(".main-offer img").Click();
			AssertText("Заявка на подключение");
			var selectedValue = browser.FindElementByCssSelector("#clientRequest_Plan.rounded option[selected='selected']");
			Assert.That(selectedValue.Text, Is.EqualTo("Народный"), "В поле тариф должен быть выбран акционный тариф");
			//Должна быть одна зеленая галочка,при поиске одного элемента вероятно выдаст ошибку,в данном случае вернет пустой массив.
			var greenElements = browser.FindElementsByCssSelector(".success .icon");
			Assert.That(greenElements.Count, Is.EqualTo(1), "Зеленая галочка должен появиться на странице");
		}
	}
}