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

namespace Inforoom2.Test.Functional.ClientRequest
{
	/// <summary>
	/// SendRequest-отправляет заявку на подключение
	/// AssertFail-функция для базовой проверки того, что данные заявки на подключение не были отправлены
	/// Setup-функция заполнения формы заявки на подключение
	/// ClientRequest- корректное и успешное заполнение заявки
	/// </summary>
	internal class ClientRequestFixture : BaseFixture
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
		/// Функция для базовой проверки того, что данные заявки на подключение не были отправлены
		/// </summary>
		/// <returns></returns>
		public void AssertFail()
		{
			AssertNoText("заявка принята");
			var request = DbSession.Query<Models.ClientRequest>().FirstOrDefault(i => i.ApplicantName == "Петров");
			Assert.That(request, Is.Null, "В базе должна не должно быть заявки");
		}

		/// <summary>
		/// Функция заполнения формы заявки на подключение
		/// </summary>
		[SetUp]
		public void Setup()
		{
			Open("ClientRequest");
			Thread.Sleep(5000);
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
		[Test, Description("Успешное заполнение заявки")]
		public void ClientRequest()
		{
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

			//забираем данные из базы данных,что бы в дальнейшем проверить его поля
			var request = DbSession.Query<Models.ClientRequest>().Where(i => i.ApplicantName == "Петров").FirstOrDefault();
			Assert.That(request, Is.Not.Null, "В базе должна сохраниться модель");

			//проверка что в базе данных все заполнено корректно
			Assert.That(request.ApplicantName, Is.EqualTo("Петров"));
			Assert.That(request.ApplicantPhoneNumber, Is.EqualTo("8556478977"));
			Assert.That(request.Email, Is.EqualTo("petrov@mail.ru"));
			Assert.That(request.Street, Is.EqualTo("Советская"));
			Assert.That(request.HouseNumber, Is.EqualTo(2));
			Assert.That(request.Housing, Is.EqualTo("3"));

			//проверяем, что в базе данных поля наименования улицы и дома сохранились в формате Яндекс
			Assert.That(request.YandexStreet, Is.StringContaining(streetYandex));
			Assert.That(request.YandexHouse, Is.StringContaining(houseYandex));
		}

		[Test, Description("Не заполнено поле ФИО")]
		public void ClientRequestWrongName()
		{
			Name.Clear();
			SendRequest();
			AssertText("Введите ФИО");
			AssertFail();
		}

		[Test, Description("Не заполнено поле номера телефона")]
		public void ClientRequestEmptyPhone()
		{
			Phone.Clear();
			SendRequest();
			AssertText("Введите номер телефона");
			AssertFail();
		}

		[Test, Description("Введен номер не в десятизначном формате")]
		public void ClientRequestWrongPhone()
		{
			Phone.Clear();
			Phone.SendKeys("01234567890");
			SendRequest();
			AssertText("Введите номер в десятизначном формате");
			AssertFail();
		}

		[Test, Description("Если не заполнено поле электронной почты, то заявка все-равно принимается")]
		public void ClientRequestEmail()
		{
			Email.Clear();
			SendRequest();
			AssertText("заявка принята");
		}

		[Test, Description("Если не заполнено поле Я прочитал договор оферту ")]
		public void ClientRequestWrongContract()
		{
			browser.FindElementByCssSelector("input[id=clientRequest_IsContractAccepted]").Click();
			SendRequest();
			AssertText("Пожалуйста, подтвердите, что Вы согласны с договором-офертой");
		}

		[Test, Description("Если не заполнены поля наименования улицы и дома формата Яндекс, но заявка все-равно принимается")]
		public void ClientRequestNoYandexAddress()
		{
			//ожидания для карты Яндекс не было, соответсвенно поля улицы и дома не успели корректно заполниться
			SendRequest();
			AssertText("заявка принята");
			//забираем данные заявки из базы данных,что бы в дальнейшем проверить ее поля
			var request = DbSession.Query<Models.ClientRequest>().Where(i => i.ApplicantName == "Петров").FirstOrDefault();
			Assert.That(request, Is.Not.Null, "В базе должна сохраниться модель");

			//проверяем, что в заявке поля Яндекс сохранились некорректно (не так как должны были быть заполнены)
			Assert.That(request.YandexHouse, Is.Not.EqualTo("2"));
		}
		
	}
}