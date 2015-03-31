using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inforoom2.Models;
using Inforoom2.Test.Functional.infrastructure;
using NHibernate.Linq;
using NPOI.SS.Formula.Functions;
using NUnit.Framework;
using OpenQA.Selenium;

namespace Inforoom2.Test.Functional
{
	/// <summary>
	/// SendRequest-отправляет заявку на подключение
	/// AssertFail-функция для базовой проверки того, что данные заявки на подключение не были отправлены
	/// Setup-функция заполнения формы заявки на подключение
	/// ClientRequest- корректное и успешное заполнение заявки
	/// </summary>
	class ClientRequestFixture : BaseFixture
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
			browser.FindElementByCssSelector(".resend").Click();;
		}

		/// <summary>
		/// Функция для базовой проверки того, что данные заявки на подключение не были отправлены
		/// </summary>
		/// <returns></returns>
		public void AssertFail()
		{
			AssertNoText("заявка принята");
			var request = DbSession.Query<ClientRequest>().FirstOrDefault(i => i.ApplicantName == "Петров");
			Assert.That(request, Is.Null, "В базе должна не должно быть заявки");
		}

		/// <summary>
		/// Функция заполнения формы заявки на подключение
		/// </summary>
		[SetUp]
		public void Setup()
		{
			Open("ClientRequest");
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
			House.SendKeys("23");
			Housing.SendKeys("3");
			browser.FindElementByCssSelector("input[id=clientRequest_IsContractAccepted]").Click();
			
		}

		/// <summary>
		/// Корректное и успешное заполнение заявки
		/// </summary>
		[Test, Description("Успешное заполнение заявки")]
		public void ClientRequest()
		{
			SendRequest();
			AssertText("заявка принята");

			//забираем данные из базы данных,что бы в дальнейшем проверить его поля
			var request = DbSession.Query<ClientRequest>().Where(i => i.ApplicantName == "Петров").FirstOrDefault();
			Assert.That(request,Is.Not.Null,"В базе должна сохраниться модель");

			//проверка что в базе данных все заполнено корректно

			Assert.That(request.ApplicantName, Is.EqualTo("Петров"));
			Assert.That(request.ApplicantPhoneNumber, Is.EqualTo("8556478977"));
			Assert.That(request.Email, Is.EqualTo("petrov@mail.ru"));
			Assert.That(request.Street, Is.EqualTo("Советская"));
			Assert.That(request.HouseNumber, Is.EqualTo(23));
			Assert.That(request.Housing, Is.EqualTo("3"));
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
		public void ClientRequestWrongPhone()
		{
			Phone.Clear();
			SendRequest();
			AssertText("Введите номер в десятизначном");
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

	}
}
