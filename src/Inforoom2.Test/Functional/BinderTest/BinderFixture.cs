using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Common.Tools.Calendar;
using Inforoom2.Components;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Inforoom2.Test.Functional.BinderTest
{
	/// <summary>
	/// SendRequest-отправляет заявку на подключение
	/// AssertFail-функция для базовой проверки того, что данные заявки на подключение не были отправлены
	/// Setup-функция заполнения формы заявки на подключение
	/// ClientRequest- корректное и успешное заполнение заявки
	/// </summary>
	internal class BinderFixture : BaseFixture
	{
		public IWebElement Name;
		public IWebElement Phone;
		public IWebElement Email;
		public IWebElement Street;
		public IWebElement House;
		public IWebElement Housing;
		public IWebElement Contract;

		/// <summary>
		/// Сокращение для отправки заявки
		/// </summary>
		public void SendRequest()
		{
			browser.FindElementByCssSelector(".resend").Click();
		}

		/// <summary>
		/// Функция заполнения формы заявки на подключение
		/// </summary>
		[SetUp]
		public void Setup()
		{
			Open("ClientRequest");
			Thread.Sleep(5000);
		}

		public void FillForm()
		{
			Name = browser.FindElementByCssSelector("input[id=clientRequest_ApplicantName]");
			Phone = browser.FindElementByCssSelector("input[id=clientRequest_ApplicantPhoneNumber]");
			Email = browser.FindElementByCssSelector("input[id=clientRequest_Email]");
			Street = browser.FindElementByCssSelector("input[id=clientRequest_Street]");
			House = browser.FindElementByCssSelector("input[id=clientRequest_HouseNumber]");
			Housing = browser.FindElementByCssSelector("input[id=clientRequest_Housing]");
			Contract = browser.FindElementByCssSelector("input[id=clientRequest_IsContractAccepted]");
			Name.SendKeys("Петров");
			Phone.SendKeys("8556478977");
			Email.SendKeys("petrov@mail.ru");
			Street.SendKeys("Советская");
			House.SendKeys("2");
			Housing.SendKeys("3");
			browser.FindElementByCssSelector("input[id=clientRequest_IsContractAccepted]").Click();
		}

		/// <summary>
		/// Корректное и успешное заполнение заявки
		/// </summary>
		[Test, Description("Проверка Binder(a)")]
		public void BinderOffCheck()
		{
			var newPlanPrice = 99999;
			var formStr =
				$"<input name=\"clientRequest.Plan.Price\" type=\"hidden\" value=\"{newPlanPrice}\" /> <input name=\"binderTestOff\" type=\"hidden\" value=\"binderTestOff\" />";
			var form = browser.FindElement(By.Id("ClientRequest"));
			var formContent = form.GetAttribute("innerHTML");
			browser.ExecuteScript(
				$"document.getElementById('ClientRequest').innerHTML = '{formStr}' + document.getElementById('ClientRequest').innerHTML;");

			FillForm();

			//ожидаем карту Яндекс
			var wait = new WebDriverWait(browser, 20.Second());
			wait.Until(d => !String.IsNullOrEmpty(browser.FindElementByCssSelector("#yandexCityHidden").GetAttribute("value")));
			wait.Until(d => !String.IsNullOrEmpty(browser.FindElementByCssSelector("#yandexStreetHidden").GetAttribute("value")));
			wait.Until(d => !String.IsNullOrEmpty(browser.FindElementByCssSelector("#yandexHouseHidden").GetAttribute("value")));

			//забираем поля заполненные яндексом для дальнейшей проверки с базой данной
			var streetYandex = browser.FindElementByCssSelector("#yandexStreetHidden").GetAttribute("value");
			var houseYandex = browser.FindElementByCssSelector("#yandexHouseHidden").GetAttribute("value");

			SendRequest();
			AssertText("заявка принята");
			var HeightPricePlan = DbSession.Query<Plan>().FirstOrDefault(s => s.Price == 99999);
			Assert.That(HeightPricePlan, Is.Not.Null, "Байндер не безопасен: стоимость тарифа должна поменяться. ");
		}

		/// <summary>
		/// Корректное и успешное заполнение заявки
		/// </summary>
		[Test, Description("Проверка Binder(a)")]
		public void BinderOnCheck()
		{
			var newPlanPrice = 99999;
			var formStr =
				$"<input name=\"clientRequest.Plan.Price\" type=\"hidden\" value=\"{newPlanPrice}\" /> <input name=\"binderTestOn\" type=\"hidden\" value=\"binderTestOn\" />";
			var form = browser.FindElement(By.Id("ClientRequest"));
			var formContent = form.GetAttribute("innerHTML");
			browser.ExecuteScript(
				$"document.getElementById('ClientRequest').innerHTML = '{formStr}' + document.getElementById('ClientRequest').innerHTML;");

			FillForm();

			//ожидаем карту Яндекс
			var wait = new WebDriverWait(browser, 20.Second());
			wait.Until(d => !String.IsNullOrEmpty(browser.FindElementByCssSelector("#yandexCityHidden").GetAttribute("value")));
			wait.Until(d => !String.IsNullOrEmpty(browser.FindElementByCssSelector("#yandexStreetHidden").GetAttribute("value")));
			wait.Until(d => !String.IsNullOrEmpty(browser.FindElementByCssSelector("#yandexHouseHidden").GetAttribute("value")));

			//забираем поля заполненные яндексом для дальнейшей проверки с базой данной
			var streetYandex = browser.FindElementByCssSelector("#yandexStreetHidden").GetAttribute("value");
			var houseYandex = browser.FindElementByCssSelector("#yandexHouseHidden").GetAttribute("value");

			SendRequest();
			AssertText("заявка принята");
			var HeightPricePlan = DbSession.Query<Plan>().FirstOrDefault(s => s.Price == 99999);
			Assert.That(HeightPricePlan, Is.Null, "Байндер безопасен: стоимость тарифа не должна поменяться. ");
		}
	}
}