using System;
using System.Linq;
using Common.Tools;
using Common.Tools.Calendar;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure.Helpers;
using NHibernate.Linq;
using NPOI.HSSF.Record;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace InforoomControlPanel.Test.Functional.ClientActions
{
	internal class RegistrationLegalFixture : ClientActionsFixture
	{
		/// <summary>
		/// Сокращение для отправки регистрации
		/// </summary>
		public void SendRegistration()
		{
			browser.FindElementByCssSelector(".btn-green.save").Click();
		}

		[Test, Description("Успешная регистрация нового клиента")]
		public void RegistrationLegalClient()
		{
			var currentUrl = browser.Url;
			string Inn = "7123321212";
			Open("Client/RegistrationLegal");
			SendRegistration();
			//проверяем обязательные поля
			AssertText("Укажите регион");
			AssertText("Введите полное наименование");
			AssertText("Введите краткое наименование");
			AssertText("Введите юридический адрес");
			AssertText("Введите фактический адрес");
			AssertText("Введите ИНН");
			AssertText("Укажите контактное лицо");
			AssertText("Введите почтовый адрес");
			AssertText("Введите номер телефона");

			var client_LegalClient_Name = browser.FindElementByCssSelector("input[id=client_LegalClient_Name]");
			var client_LegalClient_ShortName = browser.FindElementByCssSelector("input[id=client_LegalClient_ShortName]");
			var client_LegalClient_LegalAddress = browser.FindElementByCssSelector("input[id=client_LegalClient_LegalAddress]");
			var client_LegalClient_ActualAddress = browser.FindElementByCssSelector("input[id=client_LegalClient_ActualAddress]");
			var client_LegalClient_MailingAddress =
				browser.FindElementByCssSelector("input[id=client_LegalClient_MailingAddress]");
			var ContactString_1 = browser.FindElementByCssSelector("input[id=ContactString_1]");
			var client_LegalClient_Inn = browser.FindElementByCssSelector("input[id=client_LegalClient_Inn]");
			var client_LegalClient_ContactPerson = browser.FindElementByCssSelector("input[id=client_LegalClient_ContactPerson]");

			var client_Name = "Администрация Борисоглебского гор. округа";
			var client_ShortName = "Администрация";
			var client_LegalAddress = "308000, г. Белгород, пр-т Гражданский, д.38";
			var client_ActualAddress = "Гражданский проспект, д. 38";
			var client_MailingAddress = "308000, г. Белгород, пр-т Гражданский, д.38";
			var ContactString = "950764107";
			var client_ContactPerson = "Савенко Дмитрий";
			var RegionDropDown = "Борисоглебск";
			var client_Status = "Зарегистрирован";

			client_LegalClient_Name.SendKeys(client_Name);
			client_LegalClient_ShortName.SendKeys(client_ShortName);
			client_LegalClient_LegalAddress.SendKeys(client_LegalAddress);
			client_LegalClient_ActualAddress.SendKeys(client_ActualAddress);
			client_LegalClient_MailingAddress.SendKeys(client_MailingAddress);
			ContactString_1.SendKeys(ContactString);
			client_LegalClient_Inn.SendKeys(Inn);
			client_LegalClient_ContactPerson.SendKeys(client_ContactPerson);
			Css("select[id=RegionDropDown]").SelectByText(RegionDropDown);

			SendRegistration();

			AssertNoText("Укажите регион");
			AssertNoText("Введите полное наименование");
			AssertNoText("Введите краткое наименование");
			AssertNoText("Введите юридический адрес");
			AssertNoText("Введите фактический адрес");
			AssertNoText("Введите ИНН");
			AssertNoText("Укажите контактное лицо");
			AssertNoText("Введите почтовый адрес");
			AssertText("Мобильный телефон указан неверно.");

			ContactString_1 = browser.FindElementByCssSelector("input[id=ContactString_1]");
			ContactString_1.Clear();
			ContactString = "950-7641072";
            ContactString_1.SendKeys(ContactString);
			SendRegistration();

			var client = DbSession.Query<Client>().FirstOrDefault(s => s.LegalClient != null && s.LegalClient.Inn == Inn);
			Assert.That(client.LegalClient.Name, Is.EqualTo(client_Name), "В базе данных не верное значение Полного наименования");
			Assert.That(client.LegalClient.ShortName, Is.EqualTo(client_ShortName), "В базе данных не верное значение Краткого наименования");
			Assert.That(client.LegalClient.LegalAddress, Is.EqualTo(client_LegalAddress), "В базе данных не верное значение Юридического адреса");
			Assert.That(client.LegalClient.ActualAddress, Is.EqualTo(client_ActualAddress), "В базе данных не верное значение Фактического адреса");
			Assert.That(client.LegalClient.MailingAddress, Is.EqualTo(client_MailingAddress), "В базе данных не верное значение Почтового адреса");
			Assert.That(client.LegalClient.Inn, Is.EqualTo(Inn), "В базе данных не верное значение ИНН");
			Assert.That(client.LegalClient.ContactPerson, Is.EqualTo(client_ContactPerson), "В базе данных не верное значение Контактного лица");
			Assert.That(client.LegalClient.Region.Name, Is.EqualTo(RegionDropDown), "В базе данных не верное значение Региона");
			Assert.That(client.Status.Name, Is.EqualTo(client_Status), "В базе данных не верное значение Статуса");
			Assert.That(client.Contacts.Count, Is.EqualTo(1), "В базе данных нет контактных данных");

			Open(currentUrl);

		}
	}
}