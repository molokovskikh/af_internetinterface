using System.Linq;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using Billing;

namespace Inforoom2.Test.Functional.Inforoom2
{

	/// <summary>
	/// Фикстура для действий, которые не относятся к какому-либо конкретному контроллеру
	/// </summary>
	public class BaseControllerFixture : BaseFixture
	{
		[Test, Description("Проверка авторизации клиента, используя его IP адрес")]
		public void CheckNetworkLogin()
		{
			var client = DbSession.Query<Client>().ToList().First(i => i.Patronymic.Contains("с низким балансом"));
			NetworkLoginForClient(client);
			Open("/");
			AssertText(client.Name);
			var cookie = GetCookie("networkClient");
			Assert.That(cookie, Is.EqualTo("true"), "У клиента нет куки залогиненого через сеть клиента");
		}

		[Test, Description("Проверка определения города")]
		public void CitySelectTest()
		{
			SetCookie("userCity", null);
			Open();
			AssertText("ВЫБЕРИТЕ ГОРОД");
			var link = browser.FindElementByCssSelector("#CityWindow .cities a");
			link.Click();
			var userCity = GetCookie("userCity");
			Assert.That(userCity, Is.EqualTo("Борисоглебск"));
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
			Assert.That(name, Is.StringContaining(clickedText), "Изначальный город должен поменяться");
		}

		[Test, Description("Проверка возможности запросить обратный звонок, так же проверяется forwarding")]
		public void CallMeBackTicket()
		{
			Open("Faq");
			browser.FindElementByCssSelector(".call").Click();
			var name = browser.FindElementByCssSelector("input[id=callMeBackTicket_Name]");
			var phone = browser.FindElementByCssSelector("input[id=callMeBackTicket_PhoneNumber]");
			var comment = browser.FindElementByName("callMeBackTicket.Text");
			name.SendKeys("Иван Петров");
			phone.SendKeys("1234567890");
			comment.SendKeys("my question");
			browser.FindElementByCssSelector(".contacting").Click();
			var callMeBackTicket = DbSession.Query<CallMeBackTicket>().FirstOrDefault(c => c.PhoneNumber == "1234567890");
			Assert.NotNull(callMeBackTicket);
			var count = DbSession.Query<CallMeBackTicket>().ToList().Count;
			Assert.That(count, Is.EqualTo(1), "Создается только одна заявка за раз");
			AssertText("Задать вопрос:");
		}

		[Test(Description = "Выбор акционного тарифа")]
		public void PromotionalPlan()
		{
			Open("");
			browser.FindElementByCssSelector(".main-offer img").Click();
			AssertText("Заявка на подключение");
			var selectedValue = browser.FindElementByCssSelector(".rounded option[selected='selected']");
			Assert.That(selectedValue.Text, Is.EqualTo("Народный"), "В поле тариф должен быть выбран акционный тариф");
			//Должна быть одна зеленая галочка,при поиске одного элемента вероятно выдаст ошибку,в данном случае вернет пустой массив.
			var greenElements = browser.FindElementsByCssSelector(".success .icon");
			Assert.That(greenElements.Count, Is.EqualTo(1), "Зеленая галочка должен появиться на странице");
		}
	}
}