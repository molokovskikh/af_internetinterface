using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.NHibernateExtentions;
using Inforoom2;
using Inforoom2.Models;
using Inforoom2.Models.Services;
using Inforoom2.Test.Infrastructure.Helpers;
using InforoomControlPanel.Test.Functional.infrastructure;
using MvcContrib.TestHelper.Ui;
using NHibernate.Linq;
using NUnit.Framework;

namespace InforoomControlPanel.Test.Functional.ClientInfo
{
	class ClientLegalOrdersFixture : ControlPanelBaseFixture
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

		/// <summary>
		/// При обычном Flush возникала ошибка, заменил на закрытие сесии - помогло.
		/// </summary>
		private void UpdateDBSession()
		{
			DbSession.Flush();
			DbSession.Close();
			DbSession = DbSession.SessionFactory.OpenSession();
			CurrentClient = DbSession.Query<Client>().First(s => s.Id == CurrentClient.Id);
		}

		[Test, Description("Страница клиента. Юр. лицо. Добавление простого заказа")]
		public void SimpleOrderAdding(int serviceCost = 333, int serviceCostCircle = 1000)
		{
			string blockName = "#emptyBlock_legalOrders ";
			string blockModelName = "#ModelForOrderEdit ";
			var orderNumber = "1";
			var serviceDescription = "Услуга РАЗ";
			var serviceDescriptionCircle = "Периодичная услуга";
			var balanceBeforeOrder = CurrentClient.Balance;

			//Проверяем открываем редактор заказа 
			browser.FindElementByCssSelector(blockName + "[data-target='#ModelForOrderEdit']").Click();
			//Порт
			WaitAjax(10);
			//Номер
			WaitForVisibleCss(blockModelName + "input[name='order.Number']", 7);
			var inputObj = browser.FindElementByCssSelector(blockModelName + "input[name='order.Number']");
			inputObj.Clear();
			inputObj.SendKeys(orderNumber);
			//Дата начала
			WaitForVisibleCss(blockModelName + "input[name='order.BeginDate']", 7);
			inputObj = browser.FindElementByCssSelector(blockModelName + "input[name='order.BeginDate']");
			inputObj.Clear();
			browser.FindElementByCssSelector(".datepicker-days .day").Click();
			browser.FindElementByCssSelector("#OrderServicesNumber").Click();

			//Добавить разовую услугу
			browser.FindElementByCssSelector(blockModelName + ".addNewElement.addOrderServiceElement").Click();
			//Описание
			WaitForVisibleCss(blockModelName + ".serviceDescription input[clone='0']", 7);
			inputObj = browser.FindElementByCssSelector(blockModelName + ".serviceDescription input[clone='0']");
			inputObj.Clear();
			inputObj.SendKeys(serviceDescription);
			//Стоимость
			WaitForVisibleCss(blockModelName + "#OrderServicesList .serviceCost input[clone='0']", 7);
			inputObj = browser.FindElementByCssSelector("#OrderServicesList .serviceCost input[clone='0']");
			inputObj.Clear();
			inputObj.SendKeys(serviceCost.ToString());
			if (serviceCostCircle != 0) {
				//Добавить периодическую услугу
				browser.FindElementByCssSelector(blockModelName + ".addNewElement.addOrderServiceElement").Click();
				//Описание
				WaitForVisibleCss(blockModelName + ".serviceDescription input[clone='1']", 7);
				inputObj = browser.FindElementByCssSelector(blockModelName + ".serviceDescription input[clone='1']");
				inputObj.Clear();
				inputObj.SendKeys(serviceDescriptionCircle);
				//Стоимость
				WaitForVisibleCss(blockModelName + "#OrderServicesList .serviceCost input[clone='1']", 7);
				inputObj = browser.FindElementByCssSelector("#OrderServicesList .serviceCost input[clone='1']");
				inputObj.Clear();
				inputObj.SendKeys(serviceCostCircle.ToString());

				inputObj = browser.FindElementByCssSelector(blockModelName + "input[name='order.OrderServices[1].IsPeriodic']");
				inputObj.Click();
				Assert.That(inputObj.GetAttribute("checked"), Is.Not.Null, "Периодичность не указана.");
			}
			//Не использовать точку подключения
			browser.FindElementByCssSelector(blockModelName + "input[name='noEndpoint']").Click();
			//сохраняем изменения
			browser.FindElementByCssSelector(blockModelName + ".btn.btn-success").Click();

			CurrentClient.PaidDay = false;
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcess(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);

			Assert.That(CurrentClient.LegalClientOrders.Count, Is.EqualTo(1),
				"Количество заказов не совпадает с должным.");
			Assert.That(CurrentClient.LegalClientOrders[0].OrderServices.Count, Is.EqualTo(serviceCostCircle != 0 ? 2 : 1),
				"Количество услуг не совпадает с должным.");

			Assert.That(CurrentClient.Balance, Is.EqualTo(balanceBeforeOrder - serviceCost),
				"Баланс клиента не совпадает с должным.");
		}

		public void OrderWithConnection(bool withStaticIp = true, string postfixOfBlock = ".orderListBorder ", ClientEndpoint endpoint = null, DateTime setFinishDate = new DateTime())
		{
			string blockName = "#emptyBlock_legalOrders ";
			string blockModelName = "#ModelForOrderEdit ";
			var orderNumber = "3377800";
			var orderStart = SystemTime.Now();
			var connectionAddress = "Услуга РАЗ";
			var serviceCost = 333;
			var ipAddress = "111.111.01.1";
			var ipMaskResult = "255.192.0.0";
			var ipMask = "10";
			var currentSwitch = endpoint == null
				? DbSession.Query<Inforoom2.Models.Switch>().FirstOrDefault(s => s.Zone.Region == CurrentClient.GetRegion())
				: endpoint.Switch;
			var currentSpeed = endpoint == null
				? DbSession.Query<Inforoom2.Models.PackageSpeed>().FirstOrDefault()
				: DbSession.Query<Inforoom2.Models.PackageSpeed>().FirstOrDefault(s => s.PackageId == endpoint.PackageId);

			//Проверяем открываем редактор заказа 
			browser.FindElementByCssSelector(blockName + "[data-target='#ModelForOrderEdit']").Click();
			if (endpoint != null) {
				WaitForVisibleCss(blockModelName + $"[name='order.EndPoint.Id'] option[value='{endpoint.Id}']", 60);
			}
			else {
				WaitAjax(60);
			}
			//Номер
			WaitForVisibleCss(blockModelName + "input[name='order.Number']", 7);
			var inputObj = browser.FindElementByCssSelector(blockModelName + "input[name='order.Number']");
			inputObj.Clear();
			inputObj.SendKeys(orderNumber);
			//Дата начала
			WaitForVisibleCss(blockModelName + "input[name='order.BeginDate']", 7);
			inputObj = browser.FindElementByCssSelector(blockModelName + "input[name='order.BeginDate']");
			inputObj.Clear();
			browser.FindElementByCssSelector(".datepicker-days .day").Click();
			if (setFinishDate != DateTime.MinValue) {
				//Дата конца
				WaitForVisibleCss(blockModelName + "input[name='order.EndDate']", 7);
				inputObj = browser.FindElementByCssSelector(blockModelName + "input[name='order.EndDate']");
				inputObj.Clear();
				inputObj.SendKeys(setFinishDate.ToString("dd.MM.yyyy"));
			}
			browser.FindElementByCssSelector(blockModelName + ".modal-title").Click();
			//Точка подключения

			//Адрес подключения
			WaitForVisibleCss(blockModelName + "input[name='order.ConnectionAddress']", 7);
			inputObj = browser.FindElementByCssSelector(blockModelName + "input[name='order.ConnectionAddress']");
			inputObj.Clear();
			inputObj.SendKeys(connectionAddress);

			if (endpoint != null) {
				Css(blockModelName + "[name='order.EndPoint.Id']").SelectByText(endpoint.Id.ToString());
				inputObj = browser.FindElementByCssSelector(blockModelName + $"[name='order.EndPoint.Id'] option[value='{endpoint.Id}']");
				if (inputObj == null || inputObj.GetAttribute("selected") == "") {
					WaitAjax(60);
				}
			}
			else {
				//Коммутатора
				Css(blockModelName + "[name='connection.Switch']")
					.SelectByText(currentSwitch.Name + " (портов: " + currentSwitch.PortCount + ")");
				WaitAjax(60);
				//Порт
				WaitForVisibleCss(blockModelName + ".port.free[title='свободный порт']");
				browser.FindElementByCssSelector(blockModelName + ".port.free[title='свободный порт']:first-child").Click();
				//Скорость
				Css(blockModelName + "[name='connection.PackageId']")
					.SelectByText(currentSpeed.SpeedInMgBitFormated + " мб/с (pid: " + currentSpeed.PackageId + ") " +
					              currentSpeed.Description);
			}

			browser.FindElementByCssSelector(blockModelName + ".modal-title").Click();
			if (withStaticIp) {
				WaitAjax(60);
				//Добавить статический ip
				browser.FindElementByCssSelector(blockModelName + ".addNewElement.addStaticIpElement").Click();
				//Описание
				WaitForVisibleCss(blockModelName + ".staticIpElement .fixedIp.text", 7);
				inputObj = browser.FindElementByCssSelector(blockModelName + ".staticIpElement .fixedIp.text");
				inputObj.Clear();
				inputObj.SendKeys(ipAddress);
				browser.FindElementByCssSelector(blockModelName + "#ipStaticTable thead").Click();

				WaitAjax(60);
				//Стоимость
				WaitForVisibleCss(blockModelName + ".staticIpElement .fixedIp.value", 7);
				inputObj = browser.FindElementByCssSelector(blockModelName + ".staticIpElement .fixedIp.value");
				inputObj.Clear();
				inputObj.SendKeys(ipMask);

				browser.FindElementByCssSelector(blockModelName + "#ipStaticTable thead").Click();
				WaitAjax(60);
				AssertText(ipMaskResult, blockModelName + "#staticIpList .staticIpElement", "Маска вычислена не верно");
			}

			inputObj = browser.FindElementByCssSelector(blockModelName + "[name='connection.Switch'] option[selected='selected']");
			if (inputObj == null || inputObj.GetAttribute("value") == "") {
				if (endpoint != null) {
					WaitForVisibleCss(blockModelName + ".port.free.client", 60);
				}
			}
			//сохранение изменений
			browser.FindElementByCssSelector(blockModelName + ".btn-success").Click();

			WaitForText("Номер лицевого счета", 20);

			//Открыть заказ
			browser.FindElementByCssSelector(blockName + postfixOfBlock + ".orderTitle").Click();
			AssertText(orderNumber, blockName, "Номер заказа сохранен неверно");
			AssertText(connectionAddress, blockName, "Адрес подключения сохранен неверно");
			AssertText(currentSwitch.Name, blockName, "Коммутатор сохранен неверно");
			AssertText("Порт 1", blockName, "Порт сохранен неверно");
			AssertText("Скорость " + currentSpeed.SpeedInMgBit + " мбит/с", blockName, "Скорость сохранена неверно");
			if (withStaticIp) {
				AssertText(ipAddress + " / " + ipMask, blockName, "IP адрес сохранен неверно");
				AssertText(ipMaskResult, blockName, "Маска сохранена неверно");
			}
		}

		[Test, Description("Страница клиента. Юр. лицо. Добавление и удаление услуги 'Отмена блокировок'")]
		public void LegalServiceWorkLawyerAddRemove()
		{
			decimal totalSum = 11000;
			var tempDate = SystemTime.Now();
			SystemTime.Now = () => tempDate.FirstDayOfMonth().AddDays(20).Date;
			//выставление начальных параметров, клиент активен, варнинг не показывается
			string blockModelName = "#ModelForActivateService ";
			var serviceEnd = SystemTime.Now().AddDays(10);
			Assert.That(CurrentClient.Disabled, Is.EqualTo(false));
			Assert.That(CurrentClient.ShowBalanceWarningPage, Is.EqualTo(false));
			//добавление заказа на большую сумму (до минуса)
			var serviceSum = totalSum - 1000; //периодическая услуга
			totalSum -= CurrentClient.Balance;
			SimpleOrderAdding(Convert.ToInt32(serviceSum));
			RunBillingProcess(CurrentClient);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			//проверка совпадения тарифа
			Assert.That(CurrentClient.GetPlan().Replace(" руб.", ""),
				Is.EqualTo(CurrentClient.LegalClientOrders.Sum(s => s.OrderServices.Where(g => g.IsPeriodic).Sum(d => d.Cost)).ToString().Replace(".", ",")));
			//клиент не активен, показываем варнинг
			Assert.That(CurrentClient.Disabled, Is.EqualTo(true));
			Assert.That(CurrentClient.ShowBalanceWarningPage, Is.EqualTo(true));
			//проверка на отсутствие услуги "отключения блокировки"
			Assert.That(CurrentClient.ClientServices.Where(s => s.Service.Id == Service.GetIdByType(typeof(WorkLawyer))).ToList().Count, Is.EqualTo(0));

			//открытие редактора сервиса "отключения блокировки"  
			browser.FindElementByCssSelector(".InfoLegal .list-group [data-target='#ModelForActivateService']").Click();
			WaitAjax(10);
			//выставление даты
			WaitForVisibleCss(blockModelName + "input[name='endDate']", 10);
			var inputObj = browser.FindElementByCssSelector(blockModelName + "input[name='endDate']");
			inputObj.Clear();
			inputObj.SendKeys(serviceEnd.ToShortDateString());
			//указание причины восстановления
			browser.FindElementByCssSelector(blockModelName + ".message").Click();
			//сохранение изменений
			browser.FindElementByCssSelector(blockModelName + ".btn.btn-success").Click();

			//проверка на наличие услуги "отключения блокировки", активности клиента, отсутствие варнинга
			DbSession.Refresh(CurrentClient);
			Assert.That(CurrentClient.Disabled, Is.EqualTo(false));
			Assert.That(CurrentClient.ShowBalanceWarningPage, Is.EqualTo(false));
			var serviceList = CurrentClient.ClientServices.Where(s => s.IsActivated && s.IsDeactivated == false && s.Service.Id == Service.GetIdByType(typeof(WorkLawyer))).ToList();
			Assert.That(serviceList.Count, Is.EqualTo(1));
			//внесение платежа, не полное восставление баланса
			var payment = new Payment() {
				Client = CurrentClient,
				Employee = Employee,
				PaidOn = SystemTime.Now(),
				RecievedOn = SystemTime.Now(),
				Sum = 5000
			};
			totalSum -= payment.Sum;
			CurrentClient.Payments.Add(payment);
			CurrentClient.PaidDay = false;
			DbSession.Save(payment);
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcess(CurrentClient);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			//платеж не должен отразиться на варнинге и состоянии клиента
			Assert.That(CurrentClient.Disabled, Is.EqualTo(false));
			Assert.That(CurrentClient.ShowBalanceWarningPage, Is.EqualTo(false));
			//проверяем состояние клиента после отработки услуги "отключения блокировки"
			SystemTime.Now = () => tempDate.FirstDayOfMonth().AddDays(31).Date;
			CurrentClient.PaidDay = false;
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcess();
			DbSession.Flush();
			DbSession.Clear();
			CurrentClient = DbSession.Query<Client>().FirstOrDefault(s => s.Id == CurrentClient.Id);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			//клиент не должен быть активен и ему отображается варнинг
			Assert.That(CurrentClient.Disabled, Is.EqualTo(true));
			Assert.That(CurrentClient.ShowBalanceWarningPage, Is.EqualTo(true));
			//внесение платежа, не полное восставление баланса
			payment = new Payment() {
				Client = CurrentClient,
				Employee = Employee,
				PaidOn = SystemTime.Now(),
				RecievedOn = SystemTime.Now(),
				Sum = 2000
			};
			totalSum -= payment.Sum;
			CurrentClient.PaidDay = false;
			CurrentClient.Payments.Add(payment);
			DbSession.Save(payment);
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcess(CurrentClient);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			//платеж не должен отразиться на варнинге и состоянии клиента
			Assert.That(CurrentClient.Disabled, Is.EqualTo(true));
			Assert.That(CurrentClient.ShowBalanceWarningPage, Is.EqualTo(true));

			//внесение платежа, полное восставление баланса
			payment = new Payment() {
				Client = CurrentClient,
				Employee = Employee,
				PaidOn = SystemTime.Now(),
				RecievedOn = SystemTime.Now(),
				Sum = 3000
			};
			totalSum -= payment.Sum;
			CurrentClient.PaidDay = false;
			CurrentClient.Payments.Add(payment);
			DbSession.Save(payment);
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcess(CurrentClient);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			Assert.That(CurrentClient.LegalClient.Balance, Is.EqualTo(totalSum));
			//проверка совпадения тарифа
			Assert.That(CurrentClient.GetPlan().Replace(" руб.", ""),
				Is.EqualTo(CurrentClient.LegalClientOrders.Sum(s => s.OrderServices.Where(g => g.IsPeriodic).Sum(d => d.Cost)).ToString().Replace(".", ",")));
			//платеж (баланс = 0) должен убрать варнинг, а состояние клиента должно стать активным
			Assert.That(CurrentClient.Disabled, Is.EqualTo(false));
			Assert.That(CurrentClient.ShowBalanceWarningPage, Is.EqualTo(false));
			SystemTime.Now = () => tempDate;
		}

		[Test, Description("Страница клиента. Юр. лицо. Редактирование адреса подключения заказа")]
		public void LegalOrderAddressChange()
		{
			string blockName = "#emptyBlock_legalOrders ";
			string blockModelName = "#ModelForUpdateConnectionAddress ";
			string newAddress = "Новый адрес";
			OrderWithConnection();

			DbSession.Refresh(CurrentClient);

			var connectionAddress = CurrentClient.LegalClientOrders.First().ConnectionAddress;
			Assert.That(connectionAddress, Is.Not.EqualTo(newAddress));

			//Проверяем открываем редактор заказа 
			browser.FindElementByCssSelector(blockName + "[data-target='#ModelForUpdateConnectionAddress']").Click();
			//Порт
			WaitAjax(10);
			//Номер
			WaitForVisibleCss(blockModelName + "input[name='newAddress']", 10);
			var inputObj = browser.FindElementByCssSelector(blockModelName + "input[name='newAddress']");
			inputObj.Clear();
			inputObj.SendKeys(newAddress);
			//сохраняем изменения
			browser.FindElementByCssSelector(blockModelName + ".btn.btn-success").Click();
			WaitForText("Номер лицевого счета", 10);
			DbSession.Refresh(CurrentClient);
			Assert.That(CurrentClient.LegalClientOrders.Where(s => s.ConnectionAddress == newAddress).ToList().Count, Is.EqualTo(1));
		}

		[Test, Description("Страница клиента. Юр. лицо. Создание и удаление заказа с подключением")]
		public void OrderWithConnectionCreate_Delete()
		{
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => !s.IsDeactivated) == 0);
			OrderWithConnection();
			RunBillingProcess(CurrentClient);

			DbSession.Flush();
			DbSession.Refresh(CurrentClient);
			var orderCurrent = CurrentClient.LegalClientOrders.First();
			DbSession.Refresh(orderCurrent);
			DbSession.Refresh(orderCurrent.EndPoint);
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => s.IsActivated && !s.IsDeactivated) == 1);
			Assert.IsTrue(!orderCurrent.EndPoint.Disabled && orderCurrent.EndPoint.IsEnabled.Value);

			//Удаление заказа
			browser.FindElementByCssSelector("#emptyBlock_legalOrders .orderListBorder .right .c-pointer.red").Click();
			WaitForText("Закрытие заказа", 10);
			//сохранение изменений
			browser.FindElementByCssSelector("#ModelForOrderRemove .btn-success").Click();
			CurrentClient.PaidDay = false;
			DbSession.Save(CurrentClient);
			DbSession.Flush();
			RunBillingProcess(CurrentClient);
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(CurrentClient.LegalClient);
			orderCurrent = CurrentClient.LegalClientOrders.First();
			DbSession.Refresh(orderCurrent);
			DbSession.Refresh(orderCurrent.EndPoint);

			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => !s.IsDeactivated) == 0);
			Assert.IsTrue(orderCurrent.EndPoint.Disabled && orderCurrent.EndPoint.IsEnabled.Value);

			Open("Client/InfoLegal/" + CurrentClient.Id);
			WaitForText("Номер лицевого счета", 10);
			AssertNoText("3377800", "#emptyBlock_legalOrders", "Заказ не отменен!");
		}

		[Test, Description("Страница клиента. Юр. лицо. Создание и редактирование заказа с подключением")]
		public void OrderWithConnectionCreate_Edit()
		{
			var blockName = "#emptyBlock_legalOrders ";
			string blockModelName = "#ModelForOrderEdit ";
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count == 0);
			var endpointTODelete = CurrentClient.Endpoints.First();
			CurrentClient.Endpoints.Remove(endpointTODelete);
			DbSession.Save(CurrentClient);
			UpdateDBSession();
			//создаем новый заказ без статических адресов
			OrderWithConnection(false);
			UpdateDBSession();
			var currentEndpoint = CurrentClient.Endpoints.First();
			DbSession.Refresh(CurrentClient);

			DbSession.Flush();

			//проверяем состояние точки подключения, она не может быть активированной и должна быть заблокированной.
			// биллинг активирует заказ, при этом разблокирует точку подключения.
			//на стадии активации точки подключения выбераются точки не деактивированные и со значением активности (IsEnabled) = null
			Assert.IsTrue(currentEndpoint.Disabled && !currentEndpoint.IsEnabled.HasValue);
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => !s.HasEndPointFutureState) == 1);

			currentEndpoint.Port = currentEndpoint.Switch.PortCount;
			currentEndpoint.Ip = null;
			currentEndpoint.PackageId = DbSession.Query<PackageSpeed>().First(s => s.PackageId != 10 && s.PackageId != 100).PackageId;

			//биллиинг
			CurrentClient.PaidDay = false;
			DbSession.Save(currentEndpoint);
			DbSession.Save(CurrentClient);
			UpdateDBSession();
			RunBillingProcess();
			UpdateDBSession();

			var orderCurrent = CurrentClient.LegalClientOrders.OrderByDescending(s => s.Id).First();
			currentEndpoint = orderCurrent.EndPoint;
			//после работы биллинга точка подключения не должна быть деактивированной и подключна
			Assert.IsTrue(!currentEndpoint.Disabled && currentEndpoint.IsEnabled.Value);
			//т.к. она новая никакого "будущего состояния" для точки подключения в заказе быть не должно
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => !s.HasEndPointFutureState) == 1);

			//создаем новый заказ с фиксированными Ip на основе имеющейся точки подключения
			OrderWithConnection(true, ".orderListBorder:last-child ", currentEndpoint);
			UpdateDBSession();
			orderCurrent = CurrentClient.LegalClientOrders.OrderByDescending(s => s.Id).First();
			currentEndpoint = orderCurrent.EndPoint;

			//у клиента не должно быть услуги "фиксированный ip"
			Assert.IsTrue(CurrentClient.ClientServices.All(s => s.Service.Id != Service.GetIdByType(typeof(FixedIp))));

			//добавляем лизу, которую потом используем для фиксированного Ip-адреса 
			var newIp = new IPAddress(1771111111);
			var newLease = new Inforoom2.Models.Lease();
			newLease.Endpoint = currentEndpoint;
			newLease.LeaseBegin = SystemTime.Now();
			newLease.Pool = currentEndpoint.Pool;
			newLease.Port = currentEndpoint.Port;
			newLease.Ip = newIp;
			newLease.LeaseEnd = SystemTime.Now().AddDays(10);
			DbSession.Save(newLease);

			//Редактирование заказа, фиксированный Ip-адрес
			Open("Client/InfoLegal/" + CurrentClient.Id);
			WaitForText("Номер лицевого счета", 10);
			WaitForVisibleCss(blockName + ".orderListBorder:last-child  .orderTitle.entypo-right-open-mini");
			browser.FindElementByCssSelector(blockName + ".orderListBorder:last-child  .orderTitle.entypo-right-open-mini").Click();
			browser.FindElementByCssSelector(blockName + ".orderListBorder:last-child  a[title='Редактировать заказ']").Click();
			WaitForText("Редактирование заказа", 10);
			WaitForText(newLease.Ip.ToString(), 30);
			//добавляем фиксированный ip-адрес
			browser.FindElementByCssSelector(blockModelName + ".createFixedIp").Click();
			SafeWaitText("Фиксированный IP " + newLease.Ip, 10);

			//сохранение изменений
			browser.FindElementByCssSelector(blockModelName + ".btn-success").Click();

			DbSession.Refresh(CurrentClient);
			CurrentClient = DbSession.Query<Client>().First(s => s.Id == CurrentClient.Id);

			//в измененном заказе долно быть "будущее состояние" для точки подключения, т.к. изменились не основные значения (коммутатор, порт)
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => s.IsActivated) == 1);
			Assert.IsTrue(CurrentClient.Endpoints.Count == 1);
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => !s.IsActivated
			                                                         && s.HasEndPointFutureState && s.EndPointFutureState.ConnectionHelper.StaticIp == newLease.Ip.ToString()) == 1);


			//биллинг
			CurrentClient.PaidDay = false;
			DbSession.Save(CurrentClient);
			UpdateDBSession();
			RunBillingProcess();
			UpdateDBSession();


			//после активации заказы не могут иметь "будущего состояния" для точек подключения
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => s.IsActivated) == 2);
			Assert.IsTrue(CurrentClient.Endpoints.Count == 1);
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => s.IsActivated
			                                                         && !s.HasEndPointFutureState) == 2);
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => s.IsActivated
			                                                         && s.EndPoint.Ip.ToString() == newLease.Ip.ToString()) == 2);

			//у клиента не должно быть услуги "фиксированный ip"
			Assert.IsTrue(CurrentClient.ClientServices.All(s => s.Service.Id != Service.GetIdByType(typeof(FixedIp))));


			UpdateDBSession();
			orderCurrent = CurrentClient.LegalClientOrders.OrderByDescending(s => s.Id).First();
			currentEndpoint = orderCurrent.EndPoint;
			Open("Client/InfoLegal/" + CurrentClient.Id);
			//создаем новый заказ с фиксированными Ip на основе имеющейся точки подключения 
			OrderWithConnection(false, ".orderListBorder:last-child ", currentEndpoint, SystemTime.Now().AddDays(6).Date);
			UpdateDBSession();
			orderCurrent = CurrentClient.LegalClientOrders.OrderByDescending(s => s.Id).First();
			currentEndpoint = orderCurrent.EndPoint;
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(orderCurrent);
			DbSession.Refresh(orderCurrent.EndPoint);


			//Редактирование заказа изменяем основные значения (коммутатор, порт), должна создаться новая точка подключения
			Open("Client/InfoLegal/" + CurrentClient.Id);
			WaitForText("Номер лицевого счета", 10);
			WaitForVisibleCss(blockName + ".orderListBorder:last-child  .orderTitle.entypo-right-open-mini");
			browser.FindElementByCssSelector(blockName + ".orderListBorder:last-child  .orderTitle.entypo-right-open-mini").Click();
			browser.FindElementByCssSelector(blockName + ".orderListBorder:last-child  a[title='Редактировать заказ']").Click();
			WaitForText("Редактирование заказа", 10);

			WaitForVisibleCss(blockModelName + $"[name='order.EndPoint.Id'] option[value='{currentEndpoint.Id}']", 60);

			Css(blockModelName + "[name='order.EndPoint.Id']").SelectByText("Новая точка подключения");
			var inputObj = browser.FindElementByCssSelector(".newClientEndpointPanel select[name='connection.Switch'] option[selected='selected']");
			Assert.IsTrue(inputObj.GetAttribute("value") == string.Empty);

			Css(blockModelName + "[name='order.EndPoint.Id']").SelectByText(currentEndpoint.Id.ToString());


			var newSwitch = DbSession.Query<Inforoom2.Models.Switch>().First(s => s.Id != currentEndpoint.Switch.Id && s.Zone.Region == CurrentClient.GetRegion());
			var currentSpeed = DbSession.Query<Inforoom2.Models.PackageSpeed>().FirstOrDefault(s => s.PackageId != currentEndpoint.PackageId);

			WaitForVisibleCss(blockModelName + ".port.free[title='свободный порт']", 60);
			//Коммутатора
			Css(blockModelName + "[name='connection.Switch']").SelectByText(newSwitch.Name + " (портов: " + newSwitch.PortCount + ")");

			//Порт
			WaitForVisibleCss(blockModelName + ".port.free[title='свободный порт']", 60);
			browser.FindElementByCssSelector(blockModelName + ".port.free[title='свободный порт']:first-child").Click();
			//Скорость
			Css(blockModelName + "[name='connection.PackageId']")
				.SelectByText(currentSpeed.SpeedInMgBitFormated + " мб/с (pid: " + currentSpeed.PackageId + ") " +
				              currentSpeed.Description);

			//сохранение изменений
			browser.FindElementByCssSelector(blockModelName + ".btn-success").Click();

			WaitForText("Номер лицевого счета", 60);

			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(orderCurrent);
			DbSession.Refresh(currentEndpoint);

			//-------------------------------------------------------------|ИЗМЕНИТЬ УСЛОВИЯ ПРОВЕРОК|----------------------------------------------------------------
			orderCurrent = CurrentClient.LegalClientOrders.OrderByDescending(s => s.Id).First();
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => !s.IsDeactivated) == 3);
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => !s.IsActivated) == 1);
			Assert.IsTrue(CurrentClient.Endpoints.Count == 2);
			Assert.IsTrue(!orderCurrent.IsActivated);
			Assert.IsTrue(orderCurrent.EndPoint.Id != currentEndpoint.Id);
			Assert.IsTrue(!orderCurrent.HasEndPointFutureState);

			//биллинг
			CurrentClient.PaidDay = false;
			DbSession.Save(currentEndpoint);
			DbSession.Save(CurrentClient);
			UpdateDBSession();
			RunBillingProcess();
			UpdateDBSession();

			//после активации заказы не могут иметь "будущего состояния" для точек подключения
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => s.IsActivated) == 3);
			Assert.IsTrue(CurrentClient.Endpoints.Count(s => !s.Disabled && s.IsEnabled.Value) == 2);

			var dateForBilling = orderCurrent.EndDate.Value.AddDays(1).Date;
			//Просрочиваем заказ,точка подключения должна деактивироваться
			SystemTime.Now = () => dateForBilling;


			//биллинг
			CurrentClient.PaidDay = false;
			DbSession.Save(CurrentClient);
			UpdateDBSession();
			RunBillingProcess();
			UpdateDBSession();

			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(orderCurrent);
			DbSession.Refresh(currentEndpoint);

			//после активации заказы не могут иметь "будущего состояния" для точек подключения
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => s.IsActivated && !s.IsDeactivated) == 2);
			Assert.IsTrue(CurrentClient.LegalClientOrders.Count(s => s.IsActivated && s.IsDeactivated) == 1);
			Assert.IsTrue(CurrentClient.Endpoints.Count == 2);
			Assert.IsTrue(CurrentClient.Endpoints.Any(s => s.Id == orderCurrent.EndPoint.Id && s.Disabled));

			UpdateDBSession();
			Open("Client/InfoLegal/" + CurrentClient.Id);
			//создаем новый заказ без статических адресов
			OrderWithConnection(false);
			UpdateDBSession();

			orderCurrent = CurrentClient.LegalClientOrders.OrderByDescending(s => s.Id).First();
			currentEndpoint = orderCurrent.EndPoint;
			DbSession.Refresh(CurrentClient);
			DbSession.Refresh(orderCurrent);
			DbSession.Refresh(orderCurrent.EndPoint);

			var newEndpointId = currentEndpoint.Id;

			var oldEndpoint = CurrentClient.Endpoints.First(s => s.Disabled == false && s.IsEnabled == true);
			Assert.That(currentEndpoint.IsEnabled, Is.EqualTo(null));
			Assert.That(currentEndpoint.Disabled, Is.EqualTo(true));

			//Редактирование заказа изменяем основные значения (коммутатор, порт), должна создаться новая точка подключения
			Open("Client/InfoLegal/" + CurrentClient.Id);
			WaitForText("Номер лицевого счета", 10);
			WaitForVisibleCss(blockName + ".orderListBorder:last-child  .orderTitle.entypo-right-open-mini");
			browser.FindElementByCssSelector(blockName + ".orderListBorder:last-child  .orderTitle.entypo-right-open-mini").Click();
			browser.FindElementByCssSelector(blockName + ".orderListBorder:last-child  a[title='Редактировать заказ']").Click();
			WaitForText("Редактирование заказа", 10);

			WaitForVisibleCss(blockModelName + $"[name='order.EndPoint.Id'] option[value='{oldEndpoint.Id}']", 60);

			WaitForVisibleCss(blockModelName + ".port.free[title='свободный порт']", 60);
			Css(blockModelName + "[name='order.EndPoint.Id']").SelectByText(oldEndpoint.Id.ToString());
			WaitAjax();
			WaitForVisibleCss(blockModelName + ".port.free[title='свободный порт']", 60);

			//сохранение изменений
			browser.FindElementByCssSelector(blockModelName + ".btn-success").Click();
			UpdateDBSession();
			DbSession.Refresh(CurrentClient);
			Assert.That(DbSession.Query<ClientEndpoint>().Any(s => s.Id == newEndpointId), Is.EqualTo(false));
			SystemTime.Now = () => DateTime.Now;
		}
	}
}