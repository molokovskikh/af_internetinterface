using System;
using System.Linq;
using System.Net;
using Common.Tools;
using Common.Web.Ui.NHibernateExtentions;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure.Helpers;
using InforoomControlPanel.Test.Functional.infrastructure;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.ClientInfo
{
	internal class ClientPhysicalFixture : ControlPanelBaseFixture
	{
		private Client CurrentClient;

		[SetUp]
		//в начале 
		public void Setup()
		{
			//получаем обычного (нормально) клиента
			CurrentClient =
				DbSession.Query<Client>()
					.FirstOrDefault(s => s.Comment == ClientCreateHelper.ClientMark.normalClient.GetDescription());
			//добавляем ему контакт
			CurrentClient.Contacts.Add(new Contact()
			{
				ContactString = "9102868651",
				Type = ContactType.SmsSending,
				Client = CurrentClient
			});
			//сохраняем
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			//обновляем страницу клиента
			Open("Client/InfoPhysical/" + CurrentClient.Id);
			//получаем обновленную модель клиента
			DbSession.Refresh(CurrentClient);
			WaitForText("Номер лицевого счета");
		}

		[Test, Description("Страница клиента. Физ. лицо. Вывод личной информации")]
		public void PrivateInfo()
		{
			string blockName = "#emptyBlock_PrivatePhysicalInfo ";

			//Номер лицевого счета
			AssertText("Номер лицевого счета " + CurrentClient.Id.ToString());
			//Город
			AssertText("Город " + CurrentClient.GetRegion().Name);
			//Адрес подключения
			AssertText("Адрес подключения " + CurrentClient.PhysicalClient.Address.GetStringForPrint(city: false).Trim());
			//Дата регистрации
			AssertText("Дата регистрации " +
			           (CurrentClient.CreationDate.HasValue ? CurrentClient.CreationDate.Value.ToString("dd.MM.yyyy") : "нет"));
			//Зарегистрировал
			AssertText("Зарегистрировал " + (CurrentClient.WhoRegistered != null && CurrentClient.WhoRegistered.Id != 0
				? CurrentClient.WhoRegistered.Name
				: "нет"));
			//тариф
			AssertText("Тариф " + (CurrentClient.PhysicalClient.Plan != null
				? CurrentClient.PhysicalClient.Plan.Name + " (" + CurrentClient.PhysicalClient.Plan.Price.ToString("0.00") + " р." +
				  (CurrentClient.GetTariffPrice() != 0 ? " / " + CurrentClient.GetTariffPrice().ToString("0.00") + " р.)" : ")")
				: "нет"));
			//Баланс
			AssertText("Баланс " + CurrentClient.PhysicalClient.Balance.ToString("0.00"));
			//Денежных средств
			AssertText("Денежных средств " + CurrentClient.PhysicalClient.MoneyBalance.ToString("0.00"));
			//Бонусов
			AssertText("Бонусов " + CurrentClient.PhysicalClient.VirtualBalance.ToString("0.00"));
			//Статус клиента
			AssertText("Статус клиента " + CurrentClient.Status.Name);
			//СМС рассылка
			AssertText("СМС рассылка " + (CurrentClient.SendSmsNotification ? "да" : "нет"));
			//Скидка
			AssertText("Скидка " + CurrentClient.Discount.ToString("0.00") + "%");
			//Дата начала расчетного периода
			AssertText("Дата начала расчетного периода " + CurrentClient.RatedPeriodDate.Value.ToShortDateString());
			//Количество бесплатных дней
			AssertText("Добровольная блокировка, доступно бесплатных дней " + CurrentClient.FreeBlockDays.ToString());
			//Подключение
			AssertText("Подключение " + "не назначено");
			//Проверен
			AssertText("Проверен " + (CurrentClient.PhysicalClient.Checked ? "да" : "нет"));
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
				//Обновляем модель клиента
				DbSession.Refresh(CurrentClient);
				DbSession.Refresh(CurrentClient.PhysicalClient);
				DbSession.Refresh(internetService);
				//Проверяем изменения
				Assert.That(CurrentClient.Status.Type, Is.EqualTo(StatusType.Worked), "Статус не совпадает.");
				Assert.That(CurrentClient.Disabled, Is.EqualTo(false), "Состояние не совпадает.");
				Assert.That(CurrentClient.AutoUnblocked, Is.EqualTo(true), "Состояние не совпадает.");
				Assert.That(CurrentClient.DebtDays, Is.EqualTo(0), "Долговые дни не совпадают.");
				Assert.That(CurrentClient.ShowBalanceWarningPage, Is.EqualTo(false), "Отображение варнинга не совпадает.");
				Assert.That(internetService.IsActivated, Is.EqualTo(true), "Сервис 'Интернет' не совпадает.");
			}
			//Статус - Отключен
			if (status == StatusType.NoWorked) {
				//запускаем биллинг
				CurrentClient.PaidDay = false;
				DbSession.Save(CurrentClient);
				RunBillingProcessPayments();
				RunBillingProcessWriteoffs();
				DbSession.Flush();
				//Обновляем модель клиента
				DbSession.Refresh(CurrentClient);
				DbSession.Refresh(CurrentClient.PhysicalClient);
				DbSession.Refresh(internetService);
				//Проверяем изменения
				Assert.That(CurrentClient.Status.Type, Is.EqualTo(StatusType.NoWorked), "Статус не совпадает.");
				Assert.That(CurrentClient.Disabled, Is.EqualTo(true), "Состояние не совпадает.");
				Assert.That(CurrentClient.AutoUnblocked, Is.EqualTo(false), "Состояние не совпадает.");
				Assert.That(CurrentClient.Discount, Is.EqualTo(0), "Скидка не совпадает.");
				Assert.That(CurrentClient.StartNoBlock, Is.Null, "Отмена блокировки не совпадает.");
				Assert.That(internetService.IsActivated, Is.EqualTo(false), "Сервис 'Интернет' не совпадает.");
				Assert.That(CurrentClient.Endpoints.Count, Is.GreaterThan(0), "Отмена блокировки не совпадает.");
			}
			//Статус - Расторгнут
			if (status == StatusType.Dissolved) {
				//запускаем биллинг
				CurrentClient.PaidDay = false;
				DbSession.Save(CurrentClient);
				RunBillingProcessPayments();
				RunBillingProcessWriteoffs();
				//Обновляем модель клиента
				DbSession.Refresh(CurrentClient);
				DbSession.Refresh(CurrentClient.PhysicalClient);
				DbSession.Refresh(internetService);
				//Проверяем изменения
				Assert.That(CurrentClient.Status.Type, Is.EqualTo(StatusType.Dissolved), "Статус не совпадает.");
				Assert.That(CurrentClient.Disabled, Is.EqualTo(true), "Состояние не совпадает.");
				Assert.That(CurrentClient.AutoUnblocked, Is.EqualTo(false), "Состояние не совпадает.");
				Assert.That(CurrentClient.Discount, Is.EqualTo(0), "Скидка не совпадает.");
				Assert.That(internetService.IsActivated, Is.EqualTo(false), "Сервис 'Интернет' не совпадает.");
				Assert.That(CurrentClient.Endpoints.Count, Is.EqualTo(0), "Отмена блокировки не совпадает.");
			}
		}

		[Test, Description("Страница клиента. Физ. лицо. Вывод личной информации")]
		public void PrivateInfoStatusChange()
		{
			string blockName = "#emptyBlock_PrivatePhysicalInfo ";
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
			//Проверки последствий смены:
			CheckStatusChangeEffect(StatusType.Worked);
		}

		[Test, Description("Страница клиента. Физ. лицо. Редактирование личной информации")]
		public void PrivateInfoEditing()
		{
			string blockName = "#emptyBlock_PrivatePhysicalInfo ";
			var surname = CurrentClient.PhysicalClient.Surname;
			var name = CurrentClient.PhysicalClient.Name;
			var patronymic = CurrentClient.PhysicalClient.Patronymic;
			var status = CurrentClient.Status;
			string redmineTask = "123123";
			var marker = 2;

			//редактируем блок
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();
			//Фамилия
			var inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.Surname']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(surname), "Фамилия не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(surname + marker);
			//Имя
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.PhysicalClient.Name']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(name), "Имя не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(name + marker);
			//Отчество
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.PhysicalClient.Patronymic']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(patronymic), "Отчество не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(patronymic + marker);
			//Статус
			var inputObjList =
				browser.FindElementsByCssSelector(blockName + "[name='clientStatus'] option[selected='selected']");
			if (inputObjList.Count > 0) {
				Assert.That(inputObjList.FirstOrDefault(s => s.Text != "").Text, Is.EqualTo((status.Type).GetDescription()),
					"Статус не совпадает.");
			}
			else {
				Assert.That(inputObjList.Count, Is.Not.EqualTo(0), "Статус не совпадает.");
			}
			Css(blockName + "[name='clientStatus']").SelectByText((status.Type + marker).GetDescription());
			//СМС рассылка
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.SendSmsNotification']");
			Assert.That(inputObj.GetAttribute("checked"), Is.Null, "СМС рассылка не совпадает.");
			inputObj.Click();
			//Задача в Redmine для клиента
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.RedmineTask']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(""), "'Задача в Redmine для клиента' не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(redmineTask + marker);
			//сохранение изменений
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();
			//редактируем блок
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();
			//Фамилия
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.Surname']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(surname + marker), "Фамилия не совпадает.");
			//Имя
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.PhysicalClient.Name']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(name + marker), "Имя не совпадает.");
			//Отчество
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.PhysicalClient.Patronymic']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(patronymic + marker), "Отчество не совпадает.");
			//Статус
			inputObjList = browser.FindElementsByCssSelector(blockName + "[name='clientStatus'] option[selected='selected']");
			if (inputObjList.Count > 0) {
				Assert.That(inputObjList.FirstOrDefault(s => s.Text != "").Text, Is.EqualTo((status.Type + marker).GetDescription()), "Статус не совпадает.");
			}
			else {
				Assert.That(inputObjList.Count, Is.Not.EqualTo(0), "Статус не совпадает.");
			}
			//СМС рассылка
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.SendSmsNotification']");
			Assert.That(inputObj.GetAttribute("checked"), Is.Not.Null, "СМС рассылка не совпадает.");
			//Задача в Redmine для клиента
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.RedmineTask']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(redmineTask + marker),
				"'Задача в Redmine для клиента' не совпадает.");
		}


		[Test, Description("Страница клиента. Физ. лицо. Редактирование адреса")]
		public void AddressEditing()
		{
			string blockName = "#emptyBlock_PrivatePhysicalInfo ";
			string blockNameNew = "#ModelForAddress ";

			var region = CurrentClient.PhysicalClient.Address.Region;
			var street = CurrentClient.PhysicalClient.Address.House.Street;
			var house = CurrentClient.PhysicalClient.Address.House;
			var apartment = CurrentClient.PhysicalClient.Address.Apartment;
			var floor = CurrentClient.PhysicalClient.Address.Floor;
			var entrance = CurrentClient.PhysicalClient.Address.Entrance;

			var houseNew = DbSession.Query<House>().FirstOrDefault(s => s != house); // зависит от региона <=========
			var streetNew = houseNew.Street;
			var regionNew = houseNew.Street.Region;
			var apartmentNew = "999";
			var floorNew = "999";
			var entranceNew = "999";

			AssertText(CurrentClient.PhysicalClient.Address.GetStringForPrint(city: false), blockName);
			browser.FindElementByCssSelector(blockName + ".addressAjaxRunner").Click();
			//ожидание
			WaitForVisibleCss(blockNameNew + "#HouseDropDown option[value='" + CurrentClient.Address.House.Id + "']", 20);
			//Регион
			Css(blockNameNew + "[id='RegionDropDown']").SelectByText(regionNew.Name);
			//ожидание
			WaitForVisibleCss("#StreetDropDown option[value='" + streetNew.Id + "']", 20);
			////Улица
			Css(blockNameNew + "[id='StreetDropDown']").SelectByText(streetNew.Name);
			//ожидание
			WaitForVisibleCss("#HouseDropDown option[value='" + houseNew.Id + "']", 20);
			//Дом
			Css(blockNameNew + "[id='HouseDropDown']").SelectByText(houseNew.Number);
			//Квартира
			var inputObj = browser.FindElementByCssSelector(blockNameNew + "input[name='apartment']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(apartment ?? ""), "Квартира не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(apartmentNew);
			//Подъезд
			inputObj = browser.FindElementByCssSelector(blockNameNew + "input[name='entrance']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(entrance), "Подъезд не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(entranceNew);
			//Этаж
			inputObj = browser.FindElementByCssSelector(blockNameNew + "input[name='floor']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(floor.ToString()), "Этаж не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(floorNew);

			//сохраняем изменения
			browser.FindElementByCssSelector(blockNameNew + ".btn.btn-success").Click();

			WaitForText("Номер лицевого счета", 10);
			DbSession.Refresh(CurrentClient.PhysicalClient.Address);
			AssertText(CurrentClient.PhysicalClient.Address.GetStringForPrint(city: false), blockName);
		}

		[Test, Description("Страница клиента. Физ. лицо. Редактирование тарифа")]
		public void PlanEditing()
		{
			string blockName = "#emptyBlock_PrivatePhysicalInfo ";
			string blockNameNew = "#ModelForPlan ";
			var currentPlan = CurrentClient.PhysicalClient.Plan;
			var anotherPlan = DbSession.Query<Plan>().FirstOrDefault(s => s != currentPlan); // зависит от региона <=========

			AssertText("Тариф " + (currentPlan != null
				? currentPlan.Name + " (" + currentPlan.Price.ToString("0.00") + " р." +
				  (CurrentClient.GetTariffPrice() != 0 ? " / " + CurrentClient.GetTariffPrice().ToString("0.00") + " р.)" : ")")
				: "нет"), blockName);

			browser.FindElementByCssSelector(blockName + "[data-target='#ModelForPlan']").Click();

			var planInHistory =
				DbSession.Query<PlanHistoryEntry>().FirstOrDefault(s => s.PlanBefore == currentPlan && s.PlanAfter == anotherPlan);
			// зависит от региона <=========
			Assert.That(planInHistory, Is.Null, "Не должно быть записей в истории.");
			//ожидание
			WaitForVisibleCss(blockNameNew + "[name='plan']", 7);

			Css(blockNameNew + "[name='plan']")
				.SelectByText(anotherPlan.Name + " (" + anotherPlan.Price.ToString("0.00") + " р.)");


			//сохраняем изменения
			browser.FindElementByCssSelector(blockNameNew + ".btn.btn-success").Click();

			AssertText("Тариф клиента успешно изменен");
			AssertText("Тариф " + (anotherPlan != null
				? anotherPlan.Name + " (" + currentPlan.Price.ToString("0.00") + " р." +
				  (CurrentClient.GetTariffPrice() != 0 ? " / " + CurrentClient.GetTariffPrice().ToString("0.00") + " р.)" : ")")
				: "нет"), blockName);

			planInHistory =
				DbSession.Query<PlanHistoryEntry>().FirstOrDefault(s => s.PlanBefore == currentPlan && s.PlanAfter == anotherPlan);
			// зависит от региона <=========
			Assert.That(planInHistory, Is.Not.Null, "Запись в истории отсутствует.");
		}

		[Test, Description("Страница клиента. Физ. лицо. Возврат скидки")]
		public void SaleEditing()
		{
			SystemTime.Now = () => DateTime.Now;
			var saleSettings = DbSession.Query<SaleSettings>().FirstOrDefault();
			var defSetttings = SaleSettings.Defaults();
			saleSettings.DaysForRepair = defSetttings.DaysForRepair;
			saleSettings.FreeDaysVoluntaryBlocking = defSetttings.FreeDaysVoluntaryBlocking;
			saleSettings.MaxSale = defSetttings.MaxSale;
			saleSettings.MinSale = defSetttings.MinSale;
			saleSettings.PeriodCount = defSetttings.PeriodCount;
			saleSettings.SaleStep = defSetttings.SaleStep;
			DbSession.Save(saleSettings);
			DbSession.Flush();

			Assert.That(CurrentClient.StartNoBlock, Is.Null);
			string blockName = "#emptyBlock_PrivatePhysicalInfo ";
			string blockNameNew = "#ModelForSale ";
			var comment = "Надо!";
			CurrentClient.Discount = 5;
			CurrentClient.WriteOffs.RemoveEach(CurrentClient.WriteOffs);
			CurrentClient.WriteOffs.Add(new WriteOff() {Sale = 8, Client = CurrentClient, MoneySum = 0});
			
      DbSession.Save(CurrentClient);
			DbSession.Flush();

			Open("Client/InfoPhysical/" + CurrentClient.Id);

			AssertText("5,00%", blockName);

			browser.FindElementByCssSelector(blockName + "[data-target='#ModelForSale']").Click();
			//ожидание
			WaitForVisibleCss(blockNameNew + "input[name='comment']", 7);
			//Комментарий
			var inputObj = browser.FindElementByCssSelector(blockNameNew + "input[name='comment']");
			inputObj.Clear();
			inputObj.SendKeys(comment);
			//сохраняем изменения
			browser.FindElementByCssSelector(blockNameNew + ".btn.btn-success").Click();

			DbSession.Refresh(CurrentClient);
			//проверка скидки и периода отсчета после отработки "восстановления скидки"
			var monthOnStart = Convert.ToInt32((CurrentClient.Discount - saleSettings.MinSale) 
				/ saleSettings.SaleStep + saleSettings.PeriodCount); //расчет периода отсчета 
			Assert.That(CurrentClient.StartNoBlock.Value, Is.EqualTo(SystemTime.Now().AddMonths(-monthOnStart).Date));
			Assert.That(CurrentClient.Discount, Is.EqualTo(8), "Скидка отсутствует.");
			//проверка скидки и периода отсчета после отработки "назначения скидки" (по дате отсчета)
			CurrentClient.Discount = 0; //сброс скидки
			CurrentClient.PaidDay = false;
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcessPayments(CurrentClient);
			RunBillingProcessWriteoffs(CurrentClient, false);
			DbSession.Refresh(CurrentClient); 
			Assert.That(CurrentClient.StartNoBlock.Value, Is.EqualTo(SystemTime.Now().AddMonths(-monthOnStart).Date));
			Assert.That(CurrentClient.Discount, Is.EqualTo(8), "Скидка отсутствует.");


		}

		[Test, Description("Страница клиента. Физ. лицо. Редактирование контактов")]
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
				Assert.That(inputObjList.FirstOrDefault(s => s.Text != "").Text, Is.EqualTo((type).GetDescription()), "Тип не совпадает.");
			}
			else {
				Assert.That(inputObjList.Count, Is.Not.EqualTo(0), "Статус не совпадает.");
			}
			//создаем новый номер и проверяем его отсутствие на форме
			phoneNumber = firstContact.ContactFormatString.Substring(1);
			phoneNumberToCheck = firstContact.ContactPhoneSplitFormat.Substring(1);
			phoneNumber = markerClone + phoneNumber;
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

		[Test, Description("Страница клиента. Физ. лицо. Добавление обращения клиента")]
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

		[Test, Description("Страница клиента. Физ. лицо. Изменение Паспортных данных клиента")]
		public void PassportDataEditing()
		{
			string blockName = "#emptyBlock_documents ";
			var birthDate = CurrentClient.PhysicalClient.BirthDate;
			var certificateType = CurrentClient.PhysicalClient.CertificateType;
			var passportDate = CurrentClient.PhysicalClient.PassportDate;
			var passportResidention = CurrentClient.PhysicalClient.PassportResidention;
			var passportSeries = CurrentClient.PhysicalClient.PassportSeries;
			var passportNumber = CurrentClient.PhysicalClient.PassportNumber;
			var registrationAddress = CurrentClient.PhysicalClient.RegistrationAddress;


			//Дата рождения
			AssertText("Дата рождения " + birthDate.ToShortDateString(), blockName);
			//Документ удостоверяющий личность
			AssertText("Документ удостоверяющий личность " + certificateType.GetDescription(), blockName);
			//Серия \ Номер паспорта
			AssertText(@"Серия \ Номер паспорта " + passportSeries + @" \ " + passportNumber, blockName);
			//Дата выдачи паспорта
			AssertText(@"Дата выдачи паспорта " + passportDate.ToShortDateString(), blockName);
			//Кем выдан
			AssertText(@"Кем выдан " + passportResidention, blockName);
			//Адрес регистрации
			AssertText(@"Адрес регистрации " + registrationAddress, blockName);
			//редактируем блок
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();

			birthDate = birthDate.AddYears(5);
			passportDate = birthDate.AddYears(-5);
			passportSeries = 9 + passportSeries.Substring(1);
			passportNumber = 9 + passportNumber.Substring(1);
			passportResidention += "999";
			registrationAddress += "999";
			certificateType = certificateType + 1;

			AssertNoText(birthDate.ToShortDateString(), blockName);
			AssertNoText(passportDate.ToShortDateString(), blockName);
			AssertNoText(passportSeries, blockName);
			AssertNoText(passportNumber, blockName);

			//Дата рождения
			var inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.PhysicalClient.BirthDate']");
			inputObj.Clear();
			inputObj.SendKeys(birthDate.ToShortDateString());
			//Документ удостоверяющий личность
			Css(blockName + "[name='client.PhysicalClient.CertificateType']")
				.SelectByText(certificateType.GetDescription());
			//Серия паспорта
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.PhysicalClient.PassportSeries']");
			inputObj.Clear();
			inputObj.SendKeys(passportSeries);
			//Номер паспорта
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.PhysicalClient.PassportNumber']");
			inputObj.Clear();
			inputObj.SendKeys(passportNumber);
			//Дата выдачи паспорта
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.PhysicalClient.PassportDate']");
			inputObj.Clear();
			inputObj.SendKeys(passportDate.ToShortDateString());
			//Кем выдан
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.PhysicalClient.PassportResidention']");
			inputObj.Clear();
			inputObj.SendKeys(passportResidention);
			//Адрес регистрации
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.PhysicalClient.RegistrationAddress']");
			inputObj.Clear();
			inputObj.SendKeys(registrationAddress);
			//Проверен
			inputObj = browser.FindElementByCssSelector(blockName + "input[name='client.PhysicalClient.Checked']");
			Assert.That(inputObj.GetAttribute("checked"), Is.Null, "Поле проверен не совпадает.");
			inputObj.Click();

			//сохраняем изменения
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();

			//Дата рождения
			AssertText("Дата рождения " + birthDate.ToShortDateString(), blockName);
			//Документ удостоверяющий личность
			AssertText("Документ удостоверяющий личность " + certificateType.GetDescription(), blockName);
			//Серия \ Номер паспорта
			AssertText(@"Серия \ Номер паспорта " + passportSeries + @" \ " + passportNumber, blockName);
			//Дата выдачи паспорта
			AssertText(@"Дата выдачи паспорта " + passportDate.ToShortDateString(), blockName);
			//Кем выдан
			AssertText(@"Кем выдан " + passportResidention, blockName);
			//Адрес регистрации
			AssertText(@"Адрес регистрации " + registrationAddress, blockName);
			//Проверен
			AssertText(@"Проверен да");
		}

		[Test, Description("Страница клиента. Физ. лицо. Добавление неопознанного звонка")]
		public void UnresolvedCallEditing()
		{
			string blockName = "#emptyBlock_unresolvedCalls ";
			string blockNameContacts = "#emptyBlock_contacts ";
			var unresolvedPhone = DbSession.Query<UnresolvedCall>().ToList();
			DbSession.DeleteEach(unresolvedPhone);

			var unresolvedPhoneNew = new UnresolvedCall() {Phone = "9999999999"};
			DbSession.Save(unresolvedPhoneNew);
			DbSession.Flush();
			Open("Client/InfoPhysical/" + CurrentClient.Id);
			AssertText(unresolvedPhoneNew.Phone, blockName);
			//ожидание
			WaitForVisibleCss(blockName + ".btn.btn-white", 7);

			browser.FindElementByCssSelector(blockName + ".btn.btn-white").Click();

			AssertNoText(unresolvedPhoneNew.Phone, blockName);
			AssertText(unresolvedPhoneNew.Phone.Insert(3, "-"), blockNameContacts);
		}

		[Test, Description("Страница клиента. Физ. лицо. Добавление платежа")]
		public void PaymentEditingAdd()
		{
			string blockName = "#emptyBlock_payments ";
			string blockNamePaymentsAdd = "#ModelForPaymentsAdd ";
			var sum = 1000;
			var comment = "Комментарий-Текстовый блок 1-я шт.";
			var clientBalance = CurrentClient.Balance;

			DbSession.DeleteEach(CurrentClient.Payments.ToList());
			DbSession.Flush();

			browser.FindElementByCssSelector(blockName + ".btn.btn-white.icon-right").Click();

			//Сумма
			var inputObj = browser.FindElementByCssSelector(blockNamePaymentsAdd + "input[name='sum']");
			//ожидание
			WaitForVisibleCss(blockNamePaymentsAdd + "input[name='sum']");
			inputObj.Clear();
			inputObj.SendKeys(sum.ToString());
			//Комментарий
			inputObj = browser.FindElementByCssSelector(blockNamePaymentsAdd + "input[name='comment']");
			inputObj.Clear();
			inputObj.SendKeys(comment);

			browser.FindElementByCssSelector(blockNamePaymentsAdd + ".btn.btn-success").Click();

			browser.FindElementByCssSelector(blockName + ".btn.btn-white.icon-right").Click();
			sum += 333;
			comment = "Комментарий-Текстовый блок 2-я шт.";

			//Сумма
			inputObj = browser.FindElementByCssSelector(blockNamePaymentsAdd + "input[name='sum']");
			WaitForVisibleCss(blockNamePaymentsAdd + "input[name='sum']");
			inputObj.Clear();
			inputObj.SendKeys(sum.ToString());
			//Комментарий
			inputObj = browser.FindElementByCssSelector(blockNamePaymentsAdd + "input[name='comment']");
			inputObj.Clear();
			inputObj.SendKeys(comment);
			//Зачислить как бонус
			inputObj = browser.FindElementByCssSelector(blockNamePaymentsAdd + "input[name='isBonus']");
			Assert.That(inputObj.GetAttribute("checked"), Is.Null, "Зачислить как бонус - не совпадает с должным.");
			inputObj.Click();

			browser.FindElementByCssSelector(blockNamePaymentsAdd + ".btn.btn-success").Click();


			Assert.That(CurrentClient.Balance, Is.LessThan(clientBalance + sum), "Баланс клиента не совпадает с должным.");

			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient.PhysicalClient);

			Assert.That(CurrentClient.Balance, Is.GreaterThan(clientBalance + sum), "Баланс клиента не совпадает с должным.");
			AssertText("Платеж успешно добавлен и ожидает обработки");
		}

		[Test, Description("Страница клиента. Физ. лицо. Отмена платежа")]
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
			DbSession.Flush();

			browser.FindElementByCssSelector(blockName + ".btn.btn-white.icon-right").Click();

			//Сумма
			var inputObj = browser.FindElementByCssSelector(blockNamePaymentsAdd + "input[name='sum']");
			//ожидание
			WaitForVisibleCss(blockNamePaymentsAdd + "input[name='sum']");
			inputObj.Clear();
			inputObj.SendKeys(sum.ToString());
			//Комментарий
			inputObj = browser.FindElementByCssSelector(blockNamePaymentsAdd + "input[name='comment']");
			inputObj.Clear();
			inputObj.SendKeys(comment);

			browser.FindElementByCssSelector(blockNamePaymentsAdd + ".btn.btn-success").Click();

			Assert.That(CurrentClient.Balance, Is.EqualTo(clientBalance), "Баланс клиента не совпадает с должным.");

			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient.PhysicalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(clientBalance + sum), "Баланс клиента не совпадает с должным.");

			Open("Client/InfoPhysical/" + CurrentClient.Id);

			browser.FindElementByCssSelector(blockName + ".payment:first-child .cancel").Click();

			//Причина
			inputObj = browser.FindElementByCssSelector(blockNamePaymentsCancel + "input[name='comment']");
			//ожидание
			WaitForVisibleCss(blockNamePaymentsCancel + "input[name='comment']");
			inputObj.Clear();
			inputObj.SendKeys(commentCancel);
			AssertText("платеж №" + CurrentClient.Payments.OrderByDescending(s => s.PaidOn).FirstOrDefault().Id,
				blockNamePaymentsCancel);

			browser.FindElementByCssSelector(blockNamePaymentsCancel + ".btn.btn-red").Click();

			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient.PhysicalClient);

			Assert.That(CurrentClient.Balance, Is.LessThan(clientBalance + sum), "Баланс клиента не совпадает с должным.");
			AssertText("успешно отменен!");
		}

		[Test, Description("Страница клиента. Физ. лицо. Перевод платежа")]
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

			browser.FindElementByCssSelector(blockName + ".btn.btn-white.icon-right").Click();

			//Сумма
			var inputObj = browser.FindElementByCssSelector(blockNamePaymentsAdd + "input[name='sum']");
			//ожидание
			WaitForVisibleCss(blockNamePaymentsAdd + "input[name='sum']");
			inputObj.Clear();
			inputObj.SendKeys(sum.ToString());
			//Комментарий
			inputObj = browser.FindElementByCssSelector(blockNamePaymentsAdd + "input[name='comment']");
			inputObj.Clear();
			inputObj.SendKeys(comment);

			browser.FindElementByCssSelector(blockNamePaymentsAdd + ".btn.btn-success").Click();

			Assert.That(CurrentClient.Balance, Is.LessThan(clientBalance + sum), "Баланс клиента не совпадает с должным.");
			Assert.That(anotherClient.Balance, Is.EqualTo(1000), "Баланс клиента не совпадает с должным.");

			RunBillingProcessPayments(CurrentClient);
			DbSession.Refresh(CurrentClient.PhysicalClient);
			DbSession.Refresh(anotherClient.PhysicalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(clientBalance + sum), "Баланс клиента не совпадает с должным.");
			Assert.That(anotherClient.Balance, Is.EqualTo(clientBalance), "Баланс клиента не совпадает с должным.");


			Open("Client/InfoPhysical/" + CurrentClient.Id);

			browser.FindElementByCssSelector(blockName + ".payment:first-child .move").Click();
			AssertNoText(anotherClient.GetName());

			inputObj = browser.FindElementByCssSelector(blockNamePaymentsMove + "input[name='clientReceiverId']");
			WaitForVisibleCss(blockNamePaymentsMove + "input[name='clientReceiverId']");
			inputObj.Clear();
			inputObj.SendKeys(anotherClient.Id.ToString());
			WaitAjax(20);
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
			DbSession.Refresh(CurrentClient.PhysicalClient);
			DbSession.Refresh(anotherClient.PhysicalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(clientBalance), "Баланс клиента не совпадает с должным.");
			Assert.That(anotherClient.Balance, Is.EqualTo(clientBalance + sum), "Баланс клиента не совпадает с должным.");
		}

		[Test, Description("Страница клиента. Физ. лицо. Добавление абонентской платы")]
		public void WriteOffEditingAddAndRemove()
		{
			CurrentClient.PaidDay = false;
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcessPayments(CurrentClient);
			RunBillingProcessWriteoffs(CurrentClient, false);
			DbSession.Refresh(CurrentClient.PhysicalClient);


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
			DbSession.Refresh(CurrentClient.PhysicalClient);

			Assert.That(CurrentClient.Balance, Is.LessThan(clientBalance - sum), "Баланс клиента не совпадает с должным.");


			Open("Client/InfoPhysical/" + CurrentClient.Id);

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

			DbSession.Refresh(CurrentClient.PhysicalClient);

			Assert.That(CurrentClient.Balance, Is.GreaterThan(clientBalance - sum), "Баланс клиента не совпадает с должным.");
		}

		[Test, Description("Страница клиента. Физ. лицо. Добавление информации по подключению")]
		public void ConnectionAdding()
		{
			var connectSum = 41;
			string blockName = "#emptyBlock_endpoint ";

			CurrentClient =
				DbSession.Query<Client>()
					.FirstOrDefault(s => s.Comment == ClientCreateHelper.ClientMark.unpluggedClient.GetDescription());
			CurrentClient.Contacts.Add(new Contact()
			{
				ContactString = "9102868651",
				Type = ContactType.SmsSending,
				Client = CurrentClient
			});
			DbSession.Save(CurrentClient);
			DbSession.Flush();

			CurrentClient.PaidDay = false;
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcessPayments(CurrentClient);
			RunBillingProcessWriteoffs(CurrentClient, false);
			DbSession.Refresh(CurrentClient.PhysicalClient);

			Open("Client/InfoPhysical/" + CurrentClient.Id);

			var currentSwitch =
				DbSession.Query<Inforoom2.Models.Switch>().FirstOrDefault(s => s.Zone.Region == CurrentClient.GetRegion());
			
			Css(blockName + "[name='connection.Switch']")
				.SelectByText(currentSwitch.Name + " (портов: " + currentSwitch.PortCount + ")");
			//ожидание
			WaitAjax(10);
			browser.FindElementByCssSelector(blockName + "[data-target='#ModelForPortSelection']").Click();
			WaitForVisibleCss(blockName + "#ModelForPortSelection .port.free:first-child",15);
			browser.FindElementByCssSelector(blockName + "#ModelForPortSelection .port.free:first-child").Click();
			WaitForVisibleCss(blockName + "#ModelForPortSelection .btn.btn-default");
			browser.FindElementByCssSelector(blockName + "#ModelForPortSelection .btn.btn-default").Click();

			WaitForVisibleCss(blockName + "input[name='connectSum']", 20);
			var inputObj = browser.FindElementByCssSelector(blockName + "input[name='connectSum']");
			inputObj.Clear();
			inputObj.SendKeys(connectSum.ToString());
			//сохранение изменений 
			WaitForHiddenCss(".modal-backdrop", 20);
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();

			DbSession.Refresh(CurrentClient);
			AssertText(CurrentClient.Endpoints.First().Id.ToString(), blockName);

			var clientEntPoint = CurrentClient.Endpoints.FirstOrDefault();
			Assert.That(clientEntPoint, Is.Not.Null, "Подключение отсутствует.");
			var payment =
				DbSession.Query<PaymentForConnect>().FirstOrDefault(p => p.EndPoint == clientEntPoint && p.Sum == connectSum);
			Assert.That(payment, Is.Not.Null, "Платеж отсутствует.");
			Assert.That(payment.Sum, Is.EqualTo(connectSum), "Сумма платежа не равна введенной сумме.");

			var currentBalance = CurrentClient.Balance;
			Assert.That(CurrentClient.Balance, Is.GreaterThan(currentBalance - connectSum),
				"Баланс клиента не совпадает с должным.");

			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcessPayments(CurrentClient);
			RunBillingProcessWriteoffs(CurrentClient, false);
			DbSession.Refresh(CurrentClient.PhysicalClient);

			Assert.That(CurrentClient.Balance, Is.EqualTo(currentBalance - connectSum), "Баланс клиента не совпадает с должным.");
		}

		[Test, Description("Страница клиента. Физ. лицо. Редактирование информации по подключению")]
		public void ConnectionEditing()
		{
			string blockName = "#emptyBlock_endpoint ";
			var currentEndPoint = CurrentClient.Endpoints.First();
			var packageSpeedList = DbSession.Query<PackageSpeed>().ToList();
			var iPfixed = currentEndPoint.LeaseList.LastOrDefault().Ip.ToString();
			var iPrented = currentEndPoint.Ip.ToString();
			var packageId = currentEndPoint.PackageId.Value;
			var port = currentEndPoint.Port;
			var pool = currentEndPoint.Pool;
			var speed = PackageSpeed.GetSpeedForPackageId(packageSpeedList, packageId);
			var currentSwitch = currentEndPoint.Switch;
			var newSwitch =
				DbSession.Query<Inforoom2.Models.Switch>()
					.FirstOrDefault(s => s.Zone.Region == CurrentClient.GetRegion() && s.Id != currentSwitch.Id);
			var newIp = new IPAddress(1771111111);

			//Проверяем вывод содержимого формы
			//Редактируем 
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();
			//Фиксированный IP
			AssertText("Фиксированный IP " + iPfixed, blockName);
			//Арендованный IP
			AssertText("Арендованный IP " + iPrented, blockName);
			//Скорость
			AssertText("Скорость " + speed.ToString() + " мбит/с", blockName);
			//Порт
			var inputObj = browser.FindElementByCssSelector(blockName + "input[name='connection_Port']");
			Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(port.ToString()), "Порт не совпадает.");
			//Коммутатор
			var inputObjList =
				browser.FindElementsByCssSelector(blockName + "[name='connection.Switch'] option[selected='selected']");
			if (inputObjList.Count > 0) {
				inputObj = inputObjList.FirstOrDefault(s => s.GetAttribute("value") != "" && s.GetAttribute("value") != null);
				Assert.That(inputObj.GetAttribute("value"), Is.EqualTo(currentSwitch.Id.ToString()), "Коммутатор не совпадает.");
			}
			else {
				Assert.That(inputObjList.Count, Is.Not.EqualTo(0), "Статус не совпадает.");
			}

			//Меняем значения
			Css(blockName + "[name='connection.Switch']")
				.SelectByText(newSwitch.Name + " (портов: " + newSwitch.PortCount + ")");
			//ожидание
			WaitAjax(20);
			browser.FindElementByCssSelector(blockName + "[data-target='#ModelForPortSelectionEdit']").Click();
			WaitForVisibleCss(blockName + "#ModelForPortSelectionEdit .port.free");
			browser.FindElementByCssSelector(blockName + "#ModelForPortSelectionEdit .port.free:first-child").Click();
			browser.FindElementByCssSelector(blockName + "#ModelForPortSelectionEdit .btn.btn-default").Click();
			WaitForHiddenCss(".modal-backdrop", 20);
			WaitForVisibleCss(blockName + ".removeFixedIp",20);
			WaitAjax(10);
			browser.FindElementByCssSelector(blockName + ".removeFixedIp").Click();
			WaitAjax(10);
			//сохранение изменений
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();

			DbSession.Refresh(CurrentClient);

			var newLease =
				DbSession.Query<Inforoom2.Models.Lease>()
					.FirstOrDefault(s => s.Endpoint == currentEndPoint);
			newLease.Ip = newIp;
			newLease.LeaseEnd = SystemTime.Now().AddDays(10);
			DbSession.Save(newLease);
			DbSession.Flush();

			Open("Client/InfoPhysical/" + CurrentClient.Id);
			//Редактируем 
			browser.FindElementByCssSelector(blockName + ".btn.btn-blue.lockButton").Click();
			WaitForVisibleCss(blockName + ".createFixedIp", 20);
			browser.FindElementByCssSelector(blockName + ".createFixedIp").Click();
			SafeWaitText("Фиксированный IP " + newLease.Ip.ToString(), 10);
			//сохранение изменений
			browser.FindElementByCssSelector(blockName + ".btn.btn-green").Click();

			var endpointresult = CurrentClient.Endpoints.FirstOrDefault();
			DbSession.Refresh(endpointresult);

			Assert.That(endpointresult.Ip, Is.EqualTo(newLease.Ip), "Статус не совпадает.");
		}
	}
}