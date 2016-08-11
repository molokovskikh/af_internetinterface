using System;
using System.Linq;
using System.Net;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.NHibernateExtentions;
using Inforoom2.Helpers;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure.Helpers;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium.Support.UI;

namespace InforoomControlPanel.Test.Functional.ClientInfo
{
	internal class ClientLegallFixture : ControlPanelBaseFixture
	{
		private Client CurrentClient;

		[SetUp]
		//в начале 
		public void Setup()
		{
			//получаем клиента юр.лицо
			CurrentClient =
				DbSession.Query<Client>()
					.FirstOrDefault(s => s.Comment == ClientCreateHelper.ClientMark.legalClient.GetDescription());
			//добавляем ему контакт
			CurrentClient.Contacts.Add(new Contact() {
				ContactString = "9102868651",
				Type = ContactType.SmsSending,
				Client = CurrentClient
			});
			//сохраняем
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			//обновляем страницу клиента
			Open("Client/InfoLegal/" + CurrentClient.Id);
			//получаем обновленную модель клиента
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			WaitForText("Номер лицевого счета", 10);
		}

		[Test, Description("Страница клиента. Юр. лицо. Вывод личной информации")]
		public void PrivateInfo()
		{
			string blockName = "#emptyBlock_PrivateLegalInfo ";

			//Номер лицевого счета
			AssertText("Номер лицевого счета " + CurrentClient.Id.ToString());
			//Юридический адрес
			AssertText(("Юридический адрес " + CurrentClient.LegalClient.LegalAddress).Trim());
			//Фактический адрес
			AssertText(("Фактический адрес " + CurrentClient.LegalClient.ActualAddress).Trim());
			//Почтовый адрес
			AssertText(("Почтовый адрес " + CurrentClient.LegalClient.MailingAddress).Trim());
			//Регион
			AssertText("Регион " + CurrentClient.LegalClient.Region.Name);
			//Контактное лицо
			AssertText(("Контактное лицо " + CurrentClient.LegalClient.ContactPerson).Trim());
			//ИНН
			AssertText(("ИНН " + CurrentClient.LegalClient.Inn).Trim());
			//Дата регистрации
			AssertText("Дата регистрации " +
			           (CurrentClient.CreationDate.HasValue ? CurrentClient.CreationDate.Value.ToString("dd.MM.yyyy") : "нет"));
			//Зарегистрировал
			AssertText("Зарегистрировал " + (CurrentClient.WhoRegistered != null && CurrentClient.WhoRegistered.Id != 0
				? CurrentClient.WhoRegistered.Name
				: "нет"));
			//Абонентская плата
			AssertText("Абонентская плата " +
			           (CurrentClient.LegalClient.Plan.HasValue ? CurrentClient.LegalClient.Plan.Value.ToString("0.00") : ""));
			//Баланс
			AssertText("Баланс " + CurrentClient.LegalClient.Balance.ToString("0.00"));
			//Статус клиента
			AssertText("Статус клиента " + CurrentClient.Status.Name);
		}

		/// <summary>
		/// Проверка результата изменения статуса клиента.
		/// </summary>
		/// <param name="status"></param>
		private void CheckStatusChangeEffect(StatusType status)
		{
			var internetService = CurrentClient.ClientServices.FirstOrDefault(s => (s.Service as Internet) != null);
			//Статус - Заблокирован
			if (status == StatusType.Worked) {
				//запускаем биллинг
				CurrentClient.PaidDay = false;
				DbSession.Save(CurrentClient);
				RunBillingProcessPayments();
				RunBillingProcessWriteoffs();
				RunBillingProcessClientEndpointSwitcher();
				//Обновляем модель клиента
				DbSession.Refresh(CurrentClient);
				DbSession.Refresh(CurrentClient.LegalClient);
				DbSession.Refresh(internetService);
				//Проверяем изменения
				Assert.That(CurrentClient.Status.Type, Is.EqualTo(StatusType.Worked), "Статус не совпадает.");
				Assert.That(CurrentClient.Disabled, Is.EqualTo(false), "Состояние не совпадает.");
				Assert.That(CurrentClient.AutoUnblocked, Is.EqualTo(true), "Состояние не совпадает.");
				Assert.That(CurrentClient.DebtDays, Is.EqualTo(0), "Долговые дни не совпадают.");
				Assert.That(CurrentClient.ShowBalanceWarningPage, Is.EqualTo(false), "Отображение варнинга не совпадает.");
				Assert.That(internetService.IsActivated, Is.EqualTo(true), "Сервис 'Интернет' не совпадает.");
				Assert.That(CurrentClient.Endpoints.Count(s => !s.Disabled), Is.GreaterThan(0), "Отмена блокировки не совпадает.");
				Assert.That(CurrentClient.Endpoints.Count(s => s.Disabled),
					Is.EqualTo(CurrentClient.Endpoints.Count - CurrentClient.Endpoints.Count(s => !s.Disabled)), "Отмена блокировки не совпадает.");
			}
			//Статус - Отключен
			if (status == StatusType.NoWorked) {
				//запускаем биллинг
				CurrentClient.PaidDay = false;
				DbSession.Save(CurrentClient);
				RunBillingProcessPayments();
				RunBillingProcessWriteoffs();
				RunBillingProcessClientEndpointSwitcher();
				DbSession.Flush();
				//Обновляем модель клиента
				DbSession.Refresh(CurrentClient);
				DbSession.Refresh(CurrentClient.LegalClient);
				DbSession.Refresh(internetService);
				//Проверяем изменения
				Assert.That(CurrentClient.Status.Type, Is.EqualTo(StatusType.NoWorked), "Статус не совпадает.");
				Assert.That(CurrentClient.Disabled, Is.EqualTo(true), "Состояние не совпадает.");
				Assert.That(CurrentClient.AutoUnblocked, Is.EqualTo(false), "Состояние не совпадает.");
				Assert.That(CurrentClient.Discount, Is.EqualTo(0), "Скидка не совпадает.");
				Assert.That(CurrentClient.StartNoBlock, Is.Null, "Отмена блокировки не совпадает.");
				Assert.That(internetService.IsActivated, Is.EqualTo(false), "Сервис 'Интернет' не совпадает.");
				Assert.That(CurrentClient.Endpoints.Count(s => !s.Disabled), Is.GreaterThan(0), "Отмена блокировки не совпадает.");
				Assert.That(CurrentClient.Endpoints.Count(s => s.Disabled),
					Is.EqualTo(CurrentClient.Endpoints.Count - CurrentClient.Endpoints.Count(s => !s.Disabled)), "Отмена блокировки не совпадает.");
			}
			//Статус - Расторгнут
			if (status == StatusType.Dissolved) {
				//запускаем биллинг
				CurrentClient.PaidDay = false;
				DbSession.Save(CurrentClient);
				RunBillingProcessPayments();
				RunBillingProcessWriteoffs();
				RunBillingProcessClientEndpointSwitcher();
				//Обновляем модель клиента
				DbSession.Refresh(CurrentClient);
				DbSession.Refresh(CurrentClient.LegalClient);
				DbSession.Refresh(internetService);
				//Проверяем изменения
				Assert.That(CurrentClient.Status.Type, Is.EqualTo(StatusType.Dissolved), "Статус не совпадает.");
				Assert.That(CurrentClient.Disabled, Is.EqualTo(true), "Состояние не совпадает.");
				Assert.That(CurrentClient.AutoUnblocked, Is.EqualTo(false), "Состояние не совпадает.");
				Assert.That(CurrentClient.Discount, Is.EqualTo(0), "Скидка не совпадает.");
				Assert.That(internetService.IsActivated, Is.EqualTo(false), "Сервис 'Интернет' не совпадает.");
				Assert.That(CurrentClient.Endpoints.Count(s => !s.Disabled), Is.EqualTo(0), "Отмена блокировки не совпадает.");
				Assert.That(CurrentClient.Endpoints.Count(s => s.Disabled),
					Is.EqualTo(CurrentClient.Endpoints.Count - CurrentClient.Endpoints.Count(s => !s.Disabled)), "Отмена блокировки не совпадает.");

				//Добавляем платеж, статус измениться не должен
				var payment = new Payment() {
					Client = CurrentClient,
					Employee = Employee,
					PaidOn = SystemTime.Now(),
					RecievedOn = SystemTime.Now(),
					Sum = 10000
				};
				CurrentClient.Payments.Add(payment);
				DbSession.Save(payment);
				//запускаем биллинг
				CurrentClient.PaidDay = false;
				DbSession.Save(CurrentClient);
				RunBillingProcessPayments();
				RunBillingProcessWriteoffs();
				RunBillingProcessClientEndpointSwitcher();
				//Обновляем модель клиента
				DbSession.Refresh(CurrentClient);
				DbSession.Refresh(CurrentClient.LegalClient);
				DbSession.Refresh(internetService);
				//Проверяем изменения
				Assert.That(CurrentClient.Status.Type, Is.EqualTo(StatusType.Dissolved), "Статус не совпадает.");
				Assert.That(CurrentClient.Disabled, Is.EqualTo(true), "Состояние не совпадает.");
				Assert.That(CurrentClient.AutoUnblocked, Is.EqualTo(false), "Состояние не совпадает.");
				Assert.That(CurrentClient.Discount, Is.EqualTo(0), "Скидка не совпадает.");
				Assert.That(internetService.IsActivated, Is.EqualTo(false), "Сервис 'Интернет' не совпадает.");
				Assert.That(CurrentClient.Endpoints.Count(s => !s.Disabled), Is.EqualTo(0), "Отмена блокировки не совпадает.");
				Assert.That(CurrentClient.Endpoints.Count(s => s.Disabled),
					Is.EqualTo(CurrentClient.Endpoints.Count - CurrentClient.Endpoints.Count(s => !s.Disabled)), "Отмена блокировки не совпадает.");
			}
		}

		[Test, Description("Страница клиента. Юр. лицо. Вывод личной информации")]
		public void PrivateInfoStatusChange()
		{
			string blockName = "#emptyBlock_PrivateLegalInfo ";
			//редактируем блок
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();
			//Статус - Заблокирован
			Css(blockName + "[name='clientStatus']")
				.SelectByText((StatusType.NoWorked).GetDescription());
			//сохранение изменений
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();
			//редактируем блок
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();
			//ожидание
			WaitForVisibleCss("[name='clientStatus']", 7);
			var inputObjList =
				browser.FindElementsByCssSelector(blockName + "[name='clientStatus'] option[selected='selected']");
			if (inputObjList.Count > 0) {
				Assert.That(inputObjList.FirstOrDefault(s => s.Text != "").Text, Is.EqualTo((StatusType.NoWorked).GetDescription()),
					"Статус не совпадает.");
			}
			else {
				Assert.That(inputObjList.Count, Is.Not.EqualTo(0), "Статус не совпадает.");
			}
			//Проверки последствий смены:
			//флаги, сервисы
			CheckStatusChangeEffect(StatusType.NoWorked);


			//Статус - Подключен
			Css(blockName + "[name='clientStatus']").SelectByText((StatusType.Worked).GetDescription());
			//сохранение изменений
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();
			//редактируем блок
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();

			WaitForVisibleCss("[name='clientStatus']", 7);
			inputObjList =
				browser.FindElementsByCssSelector(blockName + "[name='clientStatus'] option[selected='selected']");
			if (inputObjList.Count > 0) {
				Assert.That(inputObjList.FirstOrDefault(s => s.Text != "").Text, Is.EqualTo((StatusType.Worked).GetDescription()),
					"Статус не совпадает.");
			}
			else {
				Assert.That(inputObjList.Count, Is.Not.EqualTo(0), "Статус не совпадает.");
			}
			//Проверки последствий смены:
			CheckStatusChangeEffect(StatusType.Worked);


			//Статус - Расторгнут
			Css(blockName + "[name='clientStatus']")
				.SelectByText((StatusType.Dissolved).GetDescription());
			//Сохранение изменений
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();
			//Проверка на ошибку
			AssertText("Не указана причина изменения статуса", blockName);
			//Повтор
			//Статус - Расторгнут
			Css(blockName + "[name='clientStatus']")
				.SelectByText((StatusType.Dissolved).GetDescription());
			//Указываем причину изменения статуса
			var inputObj = browser.FindElementByCssSelector(blockName + "textarea[name='clientStatusChangeComment']");
			inputObj.Clear();
			inputObj.SendKeys("причина изменения статуса");
			//сохранение изменений
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();
			//редактируем блок
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();
			//ожидание
			WaitForVisibleCss("[name='clientStatus']", 7);
			inputObjList =
				browser.FindElementsByCssSelector(blockName + "[name='clientStatus'] option[selected='selected']");
			if (inputObjList.Count > 0) {
				Assert.That(inputObjList.FirstOrDefault(s => s.Text != "").Text, Is.EqualTo((StatusType.Dissolved).GetDescription()),
					"Статус не совпадает.");
			}
			else {
				Assert.That(inputObjList.Count, Is.Not.EqualTo(0), "Статус не совпадает.");
			}
			//Проверки последствий смены:
			CheckStatusChangeEffect(StatusType.Dissolved);

			//Статус - Подключен
			Css(blockName + "[name='clientStatus']").SelectByText((StatusType.Worked).GetDescription());
			//сохранение изменений
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();
			//редактируем блок
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();
			//ожидание
			WaitForVisibleCss("[name='clientStatus']", 7);
			inputObjList =
				browser.FindElementsByCssSelector(blockName + "[name='clientStatus'] option[selected='selected']");
			if (inputObjList.Count > 0) {
				Assert.That(inputObjList.FirstOrDefault(s => s.Text != "").Text, Is.EqualTo((StatusType.Worked).GetDescription()),
					"Статус не совпадает.");
			}
			else {
				Assert.That(inputObjList.Count, Is.Not.EqualTo(0), "Статус не совпадает.");
			}

			var oldEnpoint = CurrentClient.Endpoints.First(s => s.Disabled);
			var endpoint = new ClientEndpoint {
				PackageId = 100,
				Client = CurrentClient,
				Ip = oldEnpoint.Ip,
				Port = 22,
				Switch = oldEnpoint.Switch,
				Pool = oldEnpoint.Pool
			};
			DbSession.Save(endpoint);

			//Проверки последствий смены:
			CheckStatusChangeEffect(StatusType.Worked);
		}

		[Test, Description("Страница клиента. Юр. лицо. Редактирование личной информации")]
		public void PrivateInfoEditing()
		{
			string blockName = "#emptyBlock_PrivateLegalInfo ";
			var shortname = CurrentClient.LegalClient.ShortName;
			var name = CurrentClient.LegalClient.Name;
			var status = CurrentClient.Status;
			var region = CurrentClient.LegalClient.Region;
			var regionNew = DbSession.Query<Region>().FirstOrDefault(s => s != CurrentClient.LegalClient.Region);
			string redmineTask = "123123";
			var marker = 77879;

			//редактируем блок
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();
			//Наименование
			var inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.LegalClient.Name']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(name), "Имя не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(name + marker);
			//Краткое наименование
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.LegalClient.ShortName']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(shortname), "Краткое наименование не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(shortname + marker);
			//Юридический адрес
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.LegalClient.LegalAddress']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(CurrentClient.LegalClient.LegalAddress),
				"Юридический адрес не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(CurrentClient.LegalClient.LegalAddress + marker);
			//Фактический адрес
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.LegalClient.ActualAddress']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(CurrentClient.LegalClient.ActualAddress),
				"Фактический адрес не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(CurrentClient.LegalClient.ActualAddress + marker);
			//Почтовый адрес
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.LegalClient.MailingAddress']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(CurrentClient.LegalClient.MailingAddress),
				"Почтовый адрес не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(CurrentClient.LegalClient.MailingAddress + marker);
			//Контактное лицо
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.LegalClient.ContactPerson']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(CurrentClient.LegalClient.ContactPerson),
				"Контактное лицо не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(CurrentClient.LegalClient.ContactPerson + marker);
			//ИНН
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.LegalClient.Inn']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(CurrentClient.LegalClient.Inn), "ИНН не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(CurrentClient.LegalClient.Inn + marker);
			//Статус
			var inputObjList =
				browser.FindElementsByCssSelector(blockName + "[name='clientStatus'] option[selected='selected']");
			if (inputObjList.Count > 0) {
				Assert.That(inputObjList.FirstOrDefault(s => s.Text != "").Text,
					Is.EqualTo(((object)(status.Type)).GetDescription()),
					"Статус не совпадает.");
			}
			else {
				Assert.That(inputObjList.Count, Is.Not.EqualTo(0), "Статус не совпадает.");
			}
			Css(blockName + "[name='clientStatus']").SelectByText(((StatusType)(status.Type + 2)).GetDescription());
			//Регион
			inputObjList =
				browser.FindElementsByCssSelector(blockName + "[name='client.LegalClient.Region.Id'] option[selected='selected']");
			if (inputObjList.Count > 0) {
				Assert.That(inputObjList.FirstOrDefault(s => s.Text != "").Text, Is.EqualTo(region.Name),
					"Регион не совпадает.");
			}
			else {
				Assert.That(inputObjList.Count, Is.Not.EqualTo(0), "Статус не совпадает.");
			}
			Css(blockName + "[name='client.LegalClient.Region.Id']").SelectByText(regionNew.Name);

			//Задача в Redmine для клиента
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.RedmineTask']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(""), "'Задача в Redmine для клиента' не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(redmineTask + marker);
			//сохранение изменений
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();

			DbSession.Refresh(CurrentClient.LegalClient);
			DbSession.Refresh(CurrentClient);

			Assert.That(CurrentClient.LegalClient.Name, Is.StringContaining(marker.ToString()), "Наименование не совпадает.");
			Assert.That(CurrentClient.LegalClient.ShortName, Is.StringContaining(marker.ToString()),
				"Краткое наименование не совпадает.");
			Assert.That(CurrentClient.LegalClient.LegalAddress, Is.StringContaining(marker.ToString()),
				"Юридический адрес не совпадает.");
			Assert.That(CurrentClient.LegalClient.ActualAddress, Is.StringContaining(marker.ToString()),
				"Фактический адрес не совпадает.");
			Assert.That(CurrentClient.LegalClient.MailingAddress, Is.StringContaining(marker.ToString()),
				"Почтовый адрес не совпадает.");
			Assert.That(CurrentClient.LegalClient.ContactPerson, Is.StringContaining(marker.ToString()),
				"Контактное лицо не совпадает.");
			Assert.That(CurrentClient.LegalClient.Inn, Is.StringContaining(marker.ToString()), "ИНН не совпадает.");
			Assert.That(CurrentClient.RedmineTask, Is.StringContaining(marker.ToString()),
				"Задача в Redmine для клиента не совпадает.");
			Assert.That(CurrentClient.LegalClient.Region, Is.EqualTo(regionNew), "Регион не совпадает.");
			Assert.That(CurrentClient.Status.Name, Is.EqualTo(((StatusType)(status.Type + 2)).GetDescription()),
				"Статус не совпадает.");
		}

		[Test, Description("Страница клиента. Юр. лицо. Редактирование контактов")]
		public void ContactsEditing()
		{
			string blockName = "#emptyBlock_contacts ";
			var firstContact = CurrentClient.Contacts.FirstOrDefault();
			var type = firstContact.Type;
			var typeClone = ContactType.MobilePhone;
			var marker = 2;
			var markerClone = 3;
			//новый телефон
			string phoneNumber = firstContact.ContactFormatString.Substring(1);
			phoneNumber = marker + phoneNumber;
			string phoneNumberToCheck = firstContact.ContactPhoneSplitFormat.Substring(1);
			phoneNumberToCheck = marker + phoneNumberToCheck;

			//Проверяем наличия текущего контакта пользователя на форме
			AssertText(firstContact.ContactPhoneSplitFormat + " " + firstContact.Type.GetDescription(), blockName);
			//Редактируем контакт
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();
			var inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.Contacts[0].ContactFormatString']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(firstContact.ContactString), "Контакт не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(phoneNumber);
			//Тип
			var inputObjList =
				browser.FindElementsByCssSelector(blockName + "[name='client.Contacts[0].Type'] option[selected='selected']");
			if (inputObjList.Count > 0) {
				Assert.That(inputObjList.FirstOrDefault(s => s.Text != "").Text, Is.EqualTo((type).GetDescription()),
					"Тип не совпадает.");
			}
			else {
				inputObjList =
					browser.FindElementsByCssSelector(blockName + "[name='client.Contacts[0].Type'] option");
				Assert.That(inputObjList.Count, Is.Not.EqualTo(0), "Статус не совпадает.");
			}
			//создаем новый номер и проверяем его отсутствие на форме
			phoneNumber = firstContact.ContactFormatString.Substring(1);
			phoneNumber = markerClone + phoneNumber;
			phoneNumberToCheck = firstContact.ContactPhoneSplitFormat.Substring(1);
			phoneNumberToCheck = markerClone + phoneNumberToCheck;
			AssertNoText(phoneNumberToCheck, blockName);
			//добавляем новый номер
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue").Click();
			//Контакт
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.Contacts[1].ContactFormatString']");
			inputObj.SendKeys(phoneNumber);
			//Тип
			Css(blockName + "[name='client.Contacts[1].Type']").SelectByText((typeClone).GetDescription());
			//сохраняем изменения
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();
			WaitForVisibleCss(blockName + ".btn.btn-blue.lockButton", 20);
			//проверяем наличие добавленного номера
			AssertText(phoneNumberToCheck, blockName);
			//Удаляем контакт
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();
			browser.FindElementByCssSelector(blockName + "#contactDel1").Click();
			//сохраняем изменения
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();
			//проверяем отсутствие удаленного номера
			AssertNoText(phoneNumberToCheck);
		}

		[Test, Description("Страница клиента. Юр. лицо. Добавление обращения клиента")]
		public void AppealAdding()
		{
			string blockName = "#emptyBlock_appeals ";
			var appealMessage = "NewAppealMessage";
			AssertNoText(appealMessage, blockName);
			//Имя
			var inputObj = browser.FindElementByCssSelector(blockName + "textarea[name='newUserAppeal']");
			inputObj.Clear();
			inputObj.SendKeys(appealMessage);
			//сохраняем изменения
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();
			AssertText(appealMessage, blockName);
		}

		[Test, Description("Страница клиента. Юр. лицо. Отмена платежа")]
		public void PaymentEditingRemove()
		{
			string blockName = "#emptyBlock_payments ";
			string blockNamePaymentsAdd = "#ModelForPaymentsAdd ";
			string blockNamePaymentsCancel = "#ModelForPaymentsCancel ";
			var sum = 1000;
			var comment = "Комментарий-Текстовый блок 1-я шт.";
			var commentCancel = "Надо удалить";
			var clientBalance = CurrentClient.Balance;

			DbSession.DeleteEach(CurrentClient.Payments.ToList());
			var payment = new Payment() {
				Client = CurrentClient,
				Employee = Employee,
				PaidOn = SystemTime.Now(),
				RecievedOn = SystemTime.Now(),
				Sum = sum
			};
			CurrentClient.Payments.Add(payment);
			DbSession.Save(payment);
			DbSession.Save(CurrentClient);
			DbSession.Flush();


			Assert.That(CurrentClient.Balance, Is.EqualTo(clientBalance), "Баланс клиента не совпадает с должным.");

			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(clientBalance + sum), "Баланс клиента не совпадает с должным.");

			Open("Client/InfoLegal/" + CurrentClient.Id);

			browser.FindElementByCssSelector(blockName + ".payment:first-child .cancel").Click();

			//Причина
			var inputObj = browser.FindElementByCssSelector(blockNamePaymentsCancel + "input[name='comment']");
			//ожидание
			WaitForVisibleCss(blockNamePaymentsCancel + "input[name='comment']");
			inputObj.Clear();
			inputObj.SendKeys(commentCancel);
			AssertText("платеж №" + CurrentClient.Payments.OrderByDescending(s => s.PaidOn).FirstOrDefault().Id,
				blockNamePaymentsCancel);

			browser.FindElementByCssSelector(blockNamePaymentsCancel + ".btn.btn-red").Click();

			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(clientBalance), "Баланс клиента не совпадает с должным.");
			AssertText("успешно отменен!");
		}

		[Test, Description("Страница клиента. Юр. лицо. Перевод платежа")]
		public void PaymentEditingMove()
		{
			string blockName = "#emptyBlock_payments ";
			string blockNamePaymentsAdd = "#ModelForPaymentsAdd ";
			string blockNamePaymentsMove = "#ModelForPaymentsMove ";
			var sum = 1000;
			var comment = "Комментарий-Текстовый блок 1-я шт.";
			var commentCancel = "Надо удалить";
			var clientBalance = CurrentClient.Balance;
			var anotherClient =
				DbSession.Query<Client>()
					.FirstOrDefault(s => s.Comment == ClientCreateHelper.ClientMark.nopassportClient.GetDescription());
			var anotherClientBalance = anotherClient.Balance;


			DbSession.DeleteEach(CurrentClient.Payments.ToList());
			DbSession.Flush();
			Assert.That(anotherClient.Balance, Is.EqualTo(1000), "Баланс клиента не совпадает с должным.");
			var payment = new Payment() {
				Client = CurrentClient,
				Employee = Employee,
				PaidOn = SystemTime.Now(),
				RecievedOn = SystemTime.Now(),
				Sum = sum
			};
			CurrentClient.Payments.Add(payment);
			DbSession.Save(payment);
			DbSession.Save(CurrentClient);
			DbSession.Flush();

			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			DbSession.Refresh(anotherClient.PhysicalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(clientBalance + sum), "Баланс клиента не совпадает с должным.");
			Assert.That(anotherClient.Balance, Is.EqualTo(clientBalance), "Баланс клиента не совпадает с должным.");


			Open("Client/InfoLegal/" + CurrentClient.Id);

			browser.FindElementByCssSelector(blockName + ".payment:first-child .move").Click();
			AssertNoText(anotherClient.GetName());

			var inputObj = browser.FindElementByCssSelector(blockNamePaymentsMove + "input[name='clientReceiverId']");
			WaitForVisibleCss(blockNamePaymentsMove + "input[name='clientReceiverId']");
			inputObj.Clear();
			inputObj.SendKeys(anotherClient.Id.ToString());
			WaitAjax(10);
			browser.FindElementByCssSelector(blockNamePaymentsMove + "#myModalLabel").Click();
			WaitAjax(20);
			//Причина
			inputObj = browser.FindElementByCssSelector(blockNamePaymentsMove + "input[name='comment']");
			WaitForVisibleCss(blockNamePaymentsMove + "input[name='comment']");
			inputObj.Clear();
			inputObj.SendKeys(commentCancel);


			WaitForText(anotherClient.Id.ToString(), 10);

			AssertText("платеж №" + CurrentClient.Payments.OrderByDescending(s => s.PaidOn).FirstOrDefault().Id,
				blockNamePaymentsMove);

			browser.FindElementByCssSelector(blockNamePaymentsMove + ".btn.btn-success").Click();

			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			DbSession.Refresh(anotherClient.PhysicalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(clientBalance), "Баланс клиента не совпадает с должным.");
			Assert.That(anotherClient.Balance, Is.EqualTo(clientBalance + sum), "Баланс клиента не совпадает с должным.");
		}

		[Test, Description("Страница клиента. Юр. лицо. Добавление абонентской платы")]
		public void WriteOffEditingAddAndRemove()
		{
			CurrentClient.PaidDay = false;
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcessPayments(CurrentClient);
			RunBillingProcessWriteoffs(CurrentClient, false);
			DbSession.Refresh(CurrentClient.LegalClient);


			string blockName = "#emptyBlock_writeoffs ";
			string blockNameWriteOffAdd = "#ModelForWriteOffsAdd ";
			string blockNameWriteOffDelete = "#ModelForWriteOffs ";

			var sum = 500;
			var comment = "Комментарий-Текстовый блок 1-я шт.";
			var clientBalance = CurrentClient.Balance;

			WaitForVisibleCss(blockName + ".btn.btn-white.icon-right", 7);

			browser.FindElementByCssSelector(blockName + ".btn.btn-white.icon-right").Click();

			//Сумма
			WaitForVisibleCss(blockNameWriteOffAdd + "input[name='sum']", 7);
			var inputObj = browser.FindElementByCssSelector(blockNameWriteOffAdd + "input[name='sum']");
			WaitForVisibleCss(blockNameWriteOffAdd + "input[name='sum']");
			inputObj.Clear();
			inputObj.SendKeys(sum.ToString());
			//Комментарий
			WaitForVisibleCss(blockNameWriteOffAdd + "input[name='comment']", 7);
			inputObj = browser.FindElementByCssSelector(blockNameWriteOffAdd + "input[name='comment']");
			inputObj.Clear();
			inputObj.SendKeys(comment);

			browser.FindElementByCssSelector(blockNameWriteOffAdd + ".btn.btn-success").Click();

			Assert.That(CurrentClient.Balance, Is.GreaterThan(clientBalance - sum), "Баланс клиента не совпадает с должным.");

			CurrentClient.PaidDay = false;
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcessPayments(CurrentClient);
			RunBillingProcessWriteoffs(CurrentClient, false);
			DbSession.Refresh(CurrentClient.LegalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(clientBalance - sum), "Баланс клиента не совпадает с должным.");


			Open("Client/InfoLegal/" + CurrentClient.Id);

			WaitForVisibleCss(blockName + ".entypo-right-open-mini:first-child", 7);
			browser.FindElementByCssSelector(blockName + ".entypo-right-open-mini:first-child").Click();
			WaitForVisibleCss(blockName + ".c-pointer:first-child");
			var lastWriteOff =
				CurrentClient.UserWriteOffs.FirstOrDefault(s => s.Sum == 500);
			AssertText(lastWriteOff.Sum.ToString(),
				blockName + "[writeoff='" + lastWriteOff.Id + "']");


			browser.FindElementByCssSelector(blockName + "[writeoff='" + lastWriteOff.Id + "'] .c-pointer").Click();
			WaitForVisibleCss(blockNameWriteOffDelete + "input[name='comment']");

			//Комментарий
			inputObj = browser.FindElementByCssSelector(blockNameWriteOffDelete + "input[name='comment']");
			inputObj.Clear();
			inputObj.SendKeys(comment);

			AssertText("сумму: " + lastWriteOff.Sum.ToString(), blockNameWriteOffDelete);
			browser.FindElementByCssSelector(blockNameWriteOffDelete + ".btn.btn-red").Click();

			DbSession.Refresh(CurrentClient.LegalClient);

			Assert.That(CurrentClient.Balance, Is.GreaterThan(clientBalance - sum), "Баланс клиента не совпадает с должным.");
		}


	}
}