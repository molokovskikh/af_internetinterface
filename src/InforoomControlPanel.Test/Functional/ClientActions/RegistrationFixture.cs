using System;
using System.Linq;
using System.Net;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.NHibernateExtentions;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure.Helpers;
using NHibernate.Linq;
using NPOI.HSSF.Record;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace InforoomControlPanel.Test.Functional.ClientActions
{
	internal class RegistrationFixture : ClientActionsFixture
	{
		public IWebElement clientSurName;
		public IWebElement clientName;
		public IWebElement clientPatronymic;
		public IWebElement clientMobile;
		public IWebElement clientHomePhone;
		public IWebElement clientEmail;
		public IWebElement clientCityLine;
		public IWebElement clientApartment;
		public IWebElement clientEntrance;
		public IWebElement clientFloor;
		public IWebElement clientSum;
		public IWebElement clientBirthDate;
		public Agent clientAgent;
		public Client client;
		public Client clone;

		[Test, Description("Успешная регистрация нового клиента")]
		public void RegistrationClient()
		{
			Setup();
			SendRegistration();
			Open("Client/RegistrationPhysical");
			NormalClientRegisteredCheck();
		}

		private void NormalClientRegisteredCheck()
		{
			//проверяем что зарегестрированный клиент сохранился в базе данных с правильными данными
			var clientRegistration = DbSession.Query<PhysicalClient>().FirstOrDefault(p => p.Surname == "Миронов");
			Assert.That(clientRegistration, Is.Not.Null);
			Assert.That(clientRegistration.Client.Agent.Id, Is.EqualTo(clientAgent.Id), "В базе данных у зарегестированного клиента должен сохраниться агент, которого выбирали");
			Assert.That(clientRegistration.Client.Disabled, Is.True, "В базе данных у зарегестированного клиента должена сохраниться отметка-проверен");
			Assert.That(clientRegistration.BirthDate.Date.Day, Is.EqualTo(9), "В базе данных у зарегестированного клиента должена сохраниться правильно дата рождения-день");
			Assert.That(clientRegistration.BirthDate.Date.Month, Is.EqualTo(12), "В базе данных у зарегестированного клиента должена сохраниться правильно дата рождения-месяц");
			Assert.That(clientRegistration.BirthDate.Date.Year, Is.EqualTo(1990), "В базе данных у зарегестированного клиента должена сохраниться правильно дата рождения-год");
			Assert.That(clientRegistration.Plan.Name, Is.StringContaining("50 на 50"), "В базе данных у зарегестированного клиента должен сохраниться правильно тарифный план");
			Assert.That(clientRegistration.Email, Is.StringContaining("test@mail.ru"), "В базе данных у зарегестированного клиента должен сохраниться правильно адрес электронной почты");
			Assert.That(clientRegistration.ExternalClientId, Is.EqualTo(23), "В базе данных у зарегестированного клиента должен сохраниться правильно адрес электронной почты");
			Assert.That(clientRegistration.ConnectSum, Is.EqualTo(500), "В базе данных у зарегестированного клиента должена сохраниться правильно сумма за подключение");
			Assert.That(clientRegistration.Address.House.Street.Region.Name, Is.StringContaining("Борисоглебск"), "В базе данных у зарегестированного клиента должен сохраниться правильно регион");
			Assert.That(clientRegistration.Address.House.Street.Name, Is.StringContaining("улица третьяковская"), "В базе данных у зарегестированного клиента должена сохраниться правильно улица");
			Assert.That(clientRegistration.Address.House.Number, Is.StringContaining("6Б"), "В базе данных у зарегестированного клиента должен сохраниться правильно дом");
			Assert.That(clientRegistration.Address.Entrance, Is.StringContaining("5"), "В базе данных у зарегестированного клиента должен сохраниться правильно подъезд");
			Assert.That(clientRegistration.CertificateName, Is.StringContaining("паспорт"), "В базе данных у зарегестированного клиента должено сохраниться правильно название документа удостоверяющего личность");
			Assert.That(clientRegistration.PassportSeries, Is.StringContaining("1234"), "В базе данных у зарегестированного клиента должена сохраниться правильно серия паспорта ");
			Assert.That(clientRegistration.PassportNumber, Is.StringContaining("12345"), "В базе данных у зарегестированного клиента должен сохраниться правильно номер паспорта");
			Assert.That(clientRegistration.PassportDate.Day, Is.EqualTo(9), "В базе данных у зарегестированного клиента должена сохраниться правильно дата выдачи паспорта-день");
			Assert.That(clientRegistration.PassportDate.Month, Is.EqualTo(1), "В базе данных у зарегестированного клиента должена сохраниться правильно дата выдачи паспорта-месяц");
			Assert.That(clientRegistration.PassportDate.Year, Is.EqualTo(1991), "В базе данных у зарегестированного клиента должена сохраниться правильно дата выдачи паспорта-год");
			Assert.That(clientRegistration.PassportResidention, Is.StringContaining("Москва"), "В базе данных у зарегестированного клиента должено сохраниться правильно поле кем выдан");
			Assert.That(clientRegistration.RegistrationAddress, Is.StringContaining("Москва"), "В базе данных у зарегестированного клиента должен сохраниться правильно адрес регистрации");
			//проверяем что контакты сохранены в базе данных коректно
			Assert.That(clientRegistration.Client.Contacts[0].ContactString, Is.StringContaining("968-5473245"), "В базе данных у зарегестированного клиента должен сохраниться правильно мобильный телефон");
			Assert.That(clientRegistration.Client.Contacts[1].ContactString, Is.StringContaining("968-5678745"), "В базе данных у зарегестированного клиента должен сохраниться правильно домашнего телефон");
			Assert.That(clientRegistration.Client.Contacts[2].ContactString, Is.StringContaining("test@mail.ru"), "В базе данных у зарегестированного клиента должена сохраниться правильно электронная почта");
			//проверяем, что в базе данных у зарегестированного клиента правильно сохранилось имя сотрудника, который его регестрировал
			var employeeName = Environment.UserName;
			var employeeRegistration = DbSession.Query<Employee>().FirstOrDefault(p => p.Login == employeeName);
			Assert.That(clientRegistration.Client.WhoRegisteredName, Is.StringContaining(employeeRegistration.Name), "В базе данных у зарегестированного клиента должен сохраниться правильно имя сотрудника,который регестрировал");
		}

		[Test, Description("Не заполнено поле Фамилия")]
		public void ClientRegistrationWrongSurName()
		{
			Setup();
			clientSurName.Clear();
			SendRegistration();
			AssertText("Введите фамилию");
			var registration = DbSession.Query<PhysicalClient>().FirstOrDefault(p => p.Patronymic == "Дубль");
			Assert.That(registration, Is.Null, "В базе должна не должно быть регестрированного клиента");
		}

		[Test, Description("Не заполнено поле Имя")]
		public void ClientRequestWrongName()
		{
			Setup();
			clientName.Clear();
			SendRegistration();
			AssertText("Введите имя");
			var registration = DbSession.Query<PhysicalClient>().FirstOrDefault(p => p.Surname == "Миронов");
			Assert.That(registration, Is.Null, "В базе должна не должно быть регестрированного клиента");
		}

		[Test, Description("Не заполнено поле Отчество")]
		public void ClientRequestWrongPatronymic()
		{
			Setup();
			clientPatronymic.Clear();
			SendRegistration();
			AssertText("Введите отчество");
			var registration = DbSession.Query<PhysicalClient>().FirstOrDefault(p => p.Surname == "Миронов");
			Assert.That(registration, Is.Null, "В базе должна не должно быть регестрированного клиента");
		}

		[Test, Description("Не заполнены поля контактов")]
		public void ClientRequestWrongClientCellPhone()
		{
			Setup();
			clientMobile.Clear();
			clientHomePhone.Clear();
			clientEmail.Clear();
			SendRegistration();
			AssertText("Введите номер телефона");
			AssertFail();
		}

		[Test, Description("Проверка на формат вводимого номера телефона")]
		public void ClientRequestWrongClientPhone()
		{
			Setup();
			clientHomePhone.Clear();
			clientHomePhone.SendKeys("890931564567865");
			SendRegistration();
			AssertText("Домашний телефон указан неверно.");
			AssertFail();
		}

		[Test, Description("Не заполнено поле Регион")]
		public void ClientRequestWrongClientRegion()
		{
			Setup();
			Css("#RegionDropDown").SelectByText("");		
			SendRegistration();
			AssertFail();
		}

		[Test, Description("Не заполнено поле Улица")]
		public void ClientRequestWrongClientStreet()
		{
			Setup();
			Css("#StreetDropDown").SelectByText("");
			SendRegistration();
			AssertFail();
		}

		[Test, Description("Не заполнено поле Дом")]
		public void ClientRequestWrongClientHouse()
		{
			Setup();
			Css("#HouseDropDown").SelectByText("");
			SendRegistration();
			AssertFail();
		}

		[Test, Description("Не заполнено поле Кто привел в компанию")]
		public void ClientRequestWrongClientAgent()
		{
			Setup();
			Css("#AgentDropDown").SelectByText("");
			SendRegistration();
			AssertFail();
		}

		
		[Test, Description("Регитсрация клиента,который уже есть в базе данных")]
		public void ClientRequestWrongClientClone()
		{
			Setup();
			ClientCloneRegistration();
			SendRegistration();
			AssertFail();
			//проверяем,что вывелось сообщение об ошибке и указались Id подобных клиентов
			AssertText("Клиент с подобным ФИО уже зарегистрирован!");
			AssertText("ЛС " + client.Id);
			AssertText("ЛС " + clone.Id);
		}

		[Test, Description("Регитсрация клиента,который уже есть в базе данных без разрешения на дублирование")]
		public void ClientRequestClientNotAllowDuplicat()
		{
			Setup();
			ClientCloneRegistration();
			SendRegistration();
			//проверяем,что вывелось сообщение об ошибке и указались Id подобных клиентов
			AssertText("Клиент с подобным ФИО уже зарегистрирован!");
			//не нажимаем кнопку- разрешить дублировать и пробуем зарегестрировать клиента еще раз
			SendRegistration();

			var clientRegistration = DbSession.Query<PhysicalClient>().Where(p => p.Patronymic == "нормальный клиент").ToList().Count;
			Assert.That(clientRegistration, Is.EqualTo(2), "В базе данных не должен сохраниться дублированный клиент");
		}

		[Test, Description("Регитсрация клиента,который уже есть в базе данных c разрешением на дублирование")]
		public void ClientRequestClientAllowDuplicate()
		{
			Setup();
			ClientCloneRegistration();
			SendRegistration();
			WaitForMap();
			//проверяем,что вывелось сообщение об ошибке и указались Id подобных клиентов
			AssertText("Клиент с подобным ФИО уже зарегистрирован!");
			//нажимаем кнопку - разрешить дублирование
			browser.FindElementByCssSelector("input[id=scapeUserNameDoubling]").Click();
			SendRegistration();

			var clientRegistration = DbSession.Query<PhysicalClient>().Where(p => p.Patronymic == "нормальный клиент").ToList().Count;
			//проверяем,что в базе сохранился клон,которого создали и дубль,который разрешили сделать
			Assert.That(clientRegistration, Is.EqualTo(3), "В базе данных должен сохраниться дублированный клиент");
			Open("Client/RegistrationPhysical");
		}

		/// <summary>
		/// необходимо проверить что при создании новой улицы поле регион уже заполнено значением со страницы регистрации клиента
		///	после выбора региона на странице регистрации кликаем на кнопку "добавить улицу" и только потом забираем ссылку с этой кнопки
		///	так как она обновляется после выбора региона
		/// </summary>
		[Test, Description("Регитсрация клиента,в которой необходимо создать новую улицу")]
		public void ClientRequestCreateStreet()
		{
			Setup();
			browser.FindElementByCssSelector("a[id=addStreetButton]").Click();
			var attribute = browser.FindElementByCssSelector("a[id=addStreetButton]").GetAttribute("href");
			Open(attribute);
			var region = browser.FindElementsByCssSelector("select[id=RegionDropDown] option[selected]");
			var regionClient = region.First().Text;
			Assert.That(regionClient, Is.StringContaining("Борисоглебск"), "Поле Регион при создании новой улицы должно заполниться данными из заявки");
		}


		/// <summary>
		///необходимо проверить что при создании нового дома поле регион и улица уже заполнено значением со страницы регистрации клиента
		///после выбора региона и улицы на странице регистрации кликаем на кнопку "добавить дом" и только потом забираем ссылку с этой кнопки
		///так как она обновляется после выбора региона
		/// </summary>
		[Test, Description("Регитсрация клиента,в которой необходимо создать новый дом")]
		public void ClientRequestCreateHouse()
		{
			Setup();
			browser.FindElementByCssSelector("a[id=addHouseButton]").Click();
			var attribute = browser.FindElementByCssSelector("a[id=addHouseButton]").GetAttribute("href");
			Open(attribute);
			var region = browser.FindElementByCssSelector("input[id=cityName]").GetAttribute("value");
			var street = browser.FindElementsByCssSelector("select[id=StreetDropDown] option[selected]");
			var streetClient = street.First().Text;
			Assert.That(region, Is.StringContaining("Борисоглебск"), "Поле Регион при создании нового дома должно заполниться данными из заявки");
			Assert.That(streetClient, Is.StringContaining("улица третьяковская"), "Поле Улица при создании нового дома должно заполниться данными из заявки");
		}

		[Test, Description("Проверка правильного отображение тарифных планов по регионам при регитсрации клиентов")]
		public void ClientRequestPlan()
		{
			Setup();
			var plansNameList = browser.FindElementsByCssSelector("select[id=PlanDropDown] option").Where(p => p.Text != "").Select(p => p.Text).ToList();
			var region = DbSession.Query<Region>().FirstOrDefault(p => p.Name == "Борисоглебск");

		    //проверяем,что в базе данных присутствует регион с данным наименованием тарифа, что и в списке выводимом при регистрации
			foreach (var name in plansNameList) {
				var plan = DbSession.Query<RegionPlan>().FirstOrDefault(p => p.Region == region && p.Plan.Name == name);
				Assert.That(plan, Is.Not.Null);
			}	
		}

		/// <summary>
		/// Сокращение для отправки регистрации
		/// </summary>
		public void SendRegistration()
		{
			browser.FindElementByCssSelector(".btn-green.save").Click();
		}

		/// <summary>
		/// Функция для базовой проверки того, что клиента не зарегестрировало
		/// </summary>
		/// <returns></returns>
		public void AssertFail()
		{
			var registration = DbSession.Query<PhysicalClient>().FirstOrDefault(p => p.Surname == "Миронов");
			Assert.That(registration, Is.Null, "В базе должна не должно быть регестрированного клиента");
		}

		/// <summary>
		/// Функция для создания клона клиента 
		/// клонируем нормального клиента,для того что бы при повторной его регестрации проверить,что выведится сообщение о том,что существует уже два подобных клиента
		/// функция CloneClient хитрая и при клонировании поля,которые для клиентов могут быть различными, она подменяет
		///	в тестах необходимо,что бы поля остались теми же,поэтому переназначаем их заново,как у клиента,клон которого мы создаем
		/// </summary>
		public void ClientCloneRegistration()
		{
			client = GetClient(ClientCreateHelper.ClientMark.normalClient);
			clone = CloneClient(client, ClientCreateHelper.ClientMark.normalClient);
			clone.PhysicalClient.Surname = client.PhysicalClient.Surname;
			clone.PhysicalClient.Name = client.PhysicalClient.Name;
			clone.PhysicalClient.Patronymic = client.PhysicalClient.Patronymic;
			DbSession.Flush();
			DbSession.Save(clone);
			clientName.Clear();
			clientSurName.Clear();
			clientPatronymic.Clear();
			clientName.SendKeys(client.PhysicalClient.Name);
			clientSurName.SendKeys(client.PhysicalClient.Surname);
			clientPatronymic.SendKeys(client.PhysicalClient.Patronymic);
		}


		/// <summary>
		/// Функция заполнения формы регистрации 
		/// </summary>
		public void Setup()
		{
			Open("Client/RegistrationPhysical");
			var wait = new WebDriverWait(browser, 20.Second());
			clientSurName = browser.FindElementByCssSelector("input[id=client_PhysicalClient_Surname]");
			clientName = browser.FindElementByCssSelector("input[id=client_PhysicalClient_Name]");
			clientPatronymic = browser.FindElementByCssSelector("input[id=client_PhysicalClient_Patronymic]");
			clientMobile = browser.FindElementByCssSelector("input[id=ContactString_1]");
			clientHomePhone = browser.FindElementByCssSelector("input[id=ContactString_2]");
			clientEmail = browser.FindElementByCssSelector("input[id=ContactString_3]");
			clientCityLine = browser.FindElementByCssSelector("input[id=client_PhysicalClient_ExternalClientId]");
			clientApartment = browser.FindElementByCssSelector("input[id=apartment_id]");
			clientEntrance = browser.FindElementByCssSelector("input[id=entrance_id]");
			clientFloor = browser.FindElementByCssSelector("input[id=floor_id]");
			clientSum = browser.FindElementByCssSelector("input[id=client_PhysicalClient_ConnectSum]");
			clientBirthDate = browser.FindElementByCssSelector("input[id=client_PhysicalClient_BirthDate]");
			browser.FindElementByCssSelector("input[id=clientToBeChecked]").Click();
			clientSurName.SendKeys("Миронов");
			clientName.SendKeys("Александр");
			clientPatronymic.SendKeys("Дубль");
			clientMobile.SendKeys("968-5473245");
			clientHomePhone.SendKeys("968-5678745");
			clientEmail.SendKeys("test@mail.ru");
			clientCityLine.SendKeys("23");
			clientBirthDate.SendKeys("09.12.1990");
			Css("#RegionDropDown").SelectByText("Борисоглебск");
			wait.Until(d => browser.FindElementsByCssSelector("#StreetDropDown option").ToList().Count > 2);
			Css("#StreetDropDown").SelectByText("улица третьяковская");
			wait.Until(d => browser.FindElementsByCssSelector("#HouseDropDown option").ToList().Count > 1);
			Css("#HouseDropDown").SelectByText("6Б");
			clientApartment.SendKeys("22");
			clientEntrance.SendKeys("5");
			clientFloor.SendKeys("8");
			wait.Until(d => browser.FindElementsByCssSelector("#PlanDropDown option").ToList().Count > 2);
			Css("#PlanDropDown").SelectByText("50 на 50");
			clientSum.Clear();
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_CertificateName]").SendKeys("паспорт");
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_PassportSeries]").SendKeys("1234");
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_PassportNumber]").SendKeys("12345");
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_PassportDate]").SendKeys("09.01.1991");
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_PassportResidention]").SendKeys("Москва");
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_RegistrationAddress]").SendKeys("Москва");
			clientSum.SendKeys("500");
			//заполняем поле кто привел в компанию
			clientAgent = DbSession.Query<Agent>().FirstOrDefault(p => p.Name == "Сарычев Алексей Валерьевич");
			Css("#AgentDropDown").SelectByText(clientAgent.Name);
		}

		private Lease ClientWithEndpointLeaseCreate(bool badLease = false)
		{
			//Создание нужной лизы, которая задаст нужные настройки
			var addressIp = "127.22.22.22";
			var leasesToDelete = DbSession.Query<Lease>().ToList();
			//регион свича должен соответствовать клиенту в заявке, чтобы коммутатор прописался по лизе
			Inforoom2.Models.Switch switchCurrent = badLease ? null :
				DbSession.Query<Inforoom2.Models.Switch>().FirstOrDefault(s => s.Zone.Region.Name == "Борисоглебск");

			DbSession.DeleteEach(leasesToDelete);
			var leaseNew = new Lease()
			{
				Ip = IPAddress.Parse(addressIp),
				LeasedTo = "33-33-33-33-33-33-33-33-33-33-33",
				Switch = switchCurrent,
				Port = 3
			};
			DbSession.Save(leaseNew);
			DbSession.Flush();
			return leaseNew;
		}

		private void ClientWitchEndpointCheck(string mac, int switchId, int port = 1)
		{
			var newClientFromRequest = DbSession.Query<Client>().OrderByDescending(s => s.Id).FirstOrDefault();
			Assert.IsTrue(newClientFromRequest.Surname == "Миронов");
			Assert.IsTrue(newClientFromRequest.Status.Type == StatusType.Worked);
			Assert.IsTrue(newClientFromRequest.Internet.IsActivated == true);
			var endpoint = newClientFromRequest.Endpoints.FirstOrDefault();
			Assert.IsTrue(endpoint != null);
			Assert.IsTrue(endpoint.Mac == mac);
			Assert.IsTrue(endpoint.Port == port);
			Assert.IsTrue(endpoint.Switch.Id == switchId);
			Assert.IsTrue(newClientFromRequest.WorkingStartDate.Value.Date == DateTime.Today);
			var newPaymentForConnection = DbSession.Query<PaymentForConnect>().FirstOrDefault(s => s.EndPoint.Id == endpoint.Id);
			Assert.IsTrue(newPaymentForConnection.Sum == newClientFromRequest.PhysicalClient.ConnectSum);
		}

		[Test, Description("Успешная регистрация нового клиента с точкой подключения по лизе")]
		public void RegistrationClientWithNewEndpointByLeaseSuccess()
		{
			Setup();
			//Добавление флага "создания точки подключения"
			browser.FindElementByCssSelector("input[id=AddNewEndpointId]").Click();
			//Попытка зарегистрировать клиента
			SendRegistration();
			//Ожидание пока проставится адрес
			WaitForMap();
			//Ошибка, т.к. не прописаны настройки
			AssertText("настройки точки подключения заданы неверно для подключения типа гибрид.");

			var macText = "00-00-00-00-00-00";
			var macItem = browser.FindElementByCssSelector("input[name=mac]");
			macItem.Clear();
			macItem.SendKeys(macText);

			//Попытка зарегистрировать клиента
			SendRegistration();
			//Ожидание пока проставится адрес
			WaitForMap();
			//Ошибка, т.к. не прописаны настройки
			AssertText("настройки точки подключения заданы неверно для подключения типа гибрид.");
			//добавление лизы
			var leaseNew = ClientWithEndpointLeaseCreate();
			//очищаем форму, заполняем ее повторно
			Open("Client/RegistrationPhysical");
			Setup();
			//данные о точке подключения должны подхватится по лизе
			AssertText(leaseNew.Ip.ToString());
			AssertText(leaseNew.Mac);
			//настройки точки подключения
			var input = browser.FindElement(By.CssSelector("[name='mac']"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Mac);
			input = browser.FindElement(By.CssSelector("#SwitchDropDown"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Switch.Id.ToString());
			input = browser.FindElement(By.CssSelector("[name='port']"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Port.ToString());

			//Добавление флага "создания точки подключения"
			browser.FindElementByCssSelector("input[id=AddNewEndpointId]").Click();

			//Попытка зарегистрировать клиента
			SendRegistration();
			Open("Client/RegistrationPhysical");

			//Проверка клиента с точкой подключения
			ClientWitchEndpointCheck(leaseNew.Mac, leaseNew.Switch.Id, leaseNew.Port);
		}

		[Test, Description("Успешная регистрация нового клиента с точкой подключения по лизе без коммутатора и, как следствие, ручным вводом")]
		public void RegistrationClientWithNewEndpointBadLeaseCustomSettingsSuccess()
		{
			//добавление лизы
			var leaseNew = ClientWithEndpointLeaseCreate(true);
			//очищаем форму, заполняем ее повторно
			Open("Client/RegistrationPhysical");
			Setup();
			//данные о точке подключения должны подхватится по лизе
			AssertText(leaseNew.Ip.ToString());
			AssertText(leaseNew.Mac);
			//настройки точки подключения
			var input = browser.FindElement(By.CssSelector("[name='mac']"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Mac);
			input = browser.FindElement(By.CssSelector("#SwitchDropDown"));
			Assert.IsTrue(input.GetAttribute("value") == "");
			input = browser.FindElement(By.CssSelector("[name='port']"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Port.ToString());

			var switchItem =
				DbSession.Query<Inforoom2.Models.Switch>()
					.FirstOrDefault(s => s.Zone.Region.Name == "Борисоглебск");
			Css("#SwitchDropDown").SelectByText(switchItem.Name);
			WaitAjax();
			var macItem = browser.FindElementByCssSelector("input[name=mac]");
			macItem.Clear();
			macItem.SendKeys(leaseNew.Mac);
			browser.FindElementByCssSelector("a[data-target='#ModelForPortSelectionEdit'").Click();
			WaitForVisibleCss("#ModelForPortSelectionEdit");
			browser.FindElementByCssSelector("a[class='port free'").Click();
			Click("Закрыть");
			//Добавление флага "создания точки подключения"
			browser.FindElementByCssSelector("input[id=AddNewEndpointId]").Click();

			//Попытка зарегистрировать клиента
			SendRegistration();
			Open("Client/RegistrationPhysical");

			//При регистрации не должно создаваться точки подклбючения (т.к. отменено создание точки подключени - обычная регистрация)
			//проверяем что зарегестрированный клиент сохранился в базе данных с правильными данными
			ClientWitchEndpointCheck(leaseNew.Mac, switchItem.Id);
		}


		[Test, Description("Успешная регистрация нового клиента с точкой подключения по лизе и ручным вводом мака")]
		public void RegistrationClientWithNewEndpointCustomJustMacSettingsSuccess()
		{
			//добавление лизы
			var leaseNew = ClientWithEndpointLeaseCreate();
			//очищаем форму, заполняем ее повторно
			Open("Client/RegistrationPhysical");
			Setup();
			//данные о точке подключения должны подхватится по лизе
			AssertText(leaseNew.Ip.ToString());
			AssertText(leaseNew.Mac);
			//настройки точки подключения
			var input = browser.FindElement(By.CssSelector("[name='mac']"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Mac);
			input = browser.FindElement(By.CssSelector("#SwitchDropDown"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Switch.Id.ToString());
			input = browser.FindElement(By.CssSelector("[name='port']"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Port.ToString());

			var macText = "00-00-00-00-00-0";
			var macItem = browser.FindElementByCssSelector("input[name=mac]");
			macItem.Clear();
			macItem.SendKeys(macText);
			//Добавление флага "создания точки подключения"
			browser.FindElementByCssSelector("input[id=AddNewEndpointId]").Click();

			//Попытка зарегистрировать клиента
			SendRegistration();
			WaitForMap();
			AssertText("Неверный формат MAC-адреса! Необходим: 00-00-00-00-00-00");

			macText = "00-00-00-00-00-00";
			macItem = browser.FindElementByCssSelector("input[name=mac]");
			macItem.Clear();
			macItem.SendKeys(macText);
			SendRegistration();

			Open("Client/RegistrationPhysical");

			//Проверка клиента с точкой подключения
			ClientWitchEndpointCheck(macText, leaseNew.Switch.Id, leaseNew.Port);
		}

		[Test, Description("Успешная регистрация нового клиента с точкой подключения по лизе и ручным вводом")]
		public void RegistrationClientWithNewEndpointCustomSettingsSuccess()
		{
			//добавление лизы
			var leaseNew = ClientWithEndpointLeaseCreate();
			//очищаем форму, заполняем ее повторно
			Open("Client/RegistrationPhysical");
			Setup();
			//данные о точке подключения должны подхватится по лизе
			AssertText(leaseNew.Ip.ToString());
			AssertText(leaseNew.Mac);
			//настройки точки подключения
			var input = browser.FindElement(By.CssSelector("[name='mac']"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Mac);
			input = browser.FindElement(By.CssSelector("#SwitchDropDown"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Switch.Id.ToString());
			input = browser.FindElement(By.CssSelector("[name='port']"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Port.ToString());
			
			var macText = "00-00-00-00-00-00";
			var switchItem =
				DbSession.Query<Inforoom2.Models.Switch>()
					.FirstOrDefault(s => s.Zone.Region.Name == "Борисоглебск"  && s.Id != leaseNew.Switch.Id);
			Css("#SwitchDropDown").SelectByText(switchItem.Name);
			WaitAjax();
			var macItem = browser.FindElementByCssSelector("input[name=mac]");
			macItem.Clear();
			macItem.SendKeys(macText);
			browser.FindElementByCssSelector("a[data-target='#ModelForPortSelectionEdit'").Click();
			WaitForVisibleCss("#ModelForPortSelectionEdit");
			browser.FindElementByCssSelector("a[class='port free'").Click();
			Click("Закрыть");
			//Добавление флага "создания точки подключения"
			browser.FindElementByCssSelector("input[id=AddNewEndpointId]").Click();

			//Попытка зарегистрировать клиента
			SendRegistration();
			Open("Client/RegistrationPhysical");

			//Проверка клиента с точкой подключения
			ClientWitchEndpointCheck(macText,switchItem.Id);
		}


		[Test, Description("Успешная регистрация нового клиента с точкой подключения")]
		public void RegistrationClientWithLeaseButGeneralSuccess()
		{
			Setup();
			//Добавление флага "создания точки подключения"
			browser.FindElementByCssSelector("input[id=AddNewEndpointId]").Click();
			//Попытка зарегистрировать клиента
			SendRegistration();
			//Ожидание пока проставится адрес
			WaitForMap();
			//Ошибка, т.к. не прописаны настройки
			AssertText("настройки точки подключения заданы неверно для подключения типа гибрид.");
			//добавление лизы
			var leaseNew = ClientWithEndpointLeaseCreate();
			//очищаем форму, заполняем ее повторно
			Open("Client/RegistrationPhysical");
			Setup();
			//данные о точке подключения должны подхватится по лизе
			AssertText(leaseNew.Ip.ToString());
			AssertText(leaseNew.Mac);
			//настройки точки подключения
			var input = browser.FindElement(By.CssSelector("[name='mac']"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Mac);
			input = browser.FindElement(By.CssSelector("#SwitchDropDown"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Switch.Id.ToString());
			input = browser.FindElement(By.CssSelector("[name='port']"));
			Assert.IsTrue(input.GetAttribute("value") == leaseNew.Port.ToString());
			
			//Попытка зарегистрировать клиента
			SendRegistration();
			Open("Client/RegistrationPhysical");

			//При регистрации не должно создаваться точки подклбючения (т.к. отменено создание точки подключени - обычная регистрация)
			//проверяем что зарегестрированный клиент сохранился в базе данных с правильными данными
			NormalClientRegisteredCheck();
		}
	}
}