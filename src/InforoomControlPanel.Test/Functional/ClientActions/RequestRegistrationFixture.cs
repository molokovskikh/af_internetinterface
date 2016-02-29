using System;
using System.Linq;
using Common.Tools.Calendar;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Rhino.Mocks;

namespace InforoomControlPanel.Test.Functional.ClientActions
{
	internal class RequestRegistrationFixture : ClientActionsFixture
	{
		public ClientRequest request;
		public ClientRequest clientRequest;

		/// <summary>
		/// Создаем хорошую клиентскую заявку, где все поля заполнены, включая поля заполняемые Яндексом(улица и дом).
		/// </summary>
		public void Setup()
		{
			//создаем хорошую клиентскую заявку
			var plan = DbSession.Query<Plan>().FirstOrDefault(p => p.Name == "50 на 50");
			request = new ClientRequest();
			request.ApplicantName = "Сидоров Иван Андреевич";
			request.ApplicantPhoneNumber = "8556478977";
			request.Email = "test@mail.ru";
			request.City = "Борисоглебск";
			request.Street = "улица Гагарина";
			request.HouseNumber = 1;
			request.Housing = "А";
			request.Entrance = 2;
			request.Apartment = 13;
			request.Plan = plan;
			request.YandexStreet = "улица гагарина";
			request.YandexHouse = "1А";
			DbSession.Save(request);
		}

		/// <summary>
		/// Функция для базовой проверки того, что поля регитсрации клиента(фамилия,имя и отчество) заполнились автоматически из заявки
		/// </summary>
		/// <returns></returns>
		public void AssertRequestData()
		{
			//забираем заявку из базы данных для дальнейшей проверки
			clientRequest = DbSession.Query<ClientRequest>().FirstOrDefault(p => p.ApplicantName == "Сидоров Иван Андреевич");
			var clientNameRequest = clientRequest.ApplicantName.Split(' ');
			//проверяем,что поле Фамилия заполнилось данными из заявки
			var clientSurName = browser.FindElementByCssSelector("input[id=client_PhysicalClient_Surname]").GetAttribute("value");
			Assert.That(clientSurName, Is.StringContaining(clientNameRequest[0]), "Поле фамилии должно заполниться данными из заявки");

			//проверяем,что поле Имя заполнилось данными из заявки
			var clientName = browser.FindElementByCssSelector("input[id=client_PhysicalClient_Name]").GetAttribute("value");
			Assert.That(clientName, Is.StringContaining(clientNameRequest[1]), "Поле имени должно заполниться данными из заявки");

			//проверяем,что поле Отчество заполнилось данными из заявки
			var clientPatronymic = browser.FindElementByCssSelector("input[id=client_PhysicalClient_Patronymic]").GetAttribute("value");
			Assert.That(clientPatronymic, Is.StringContaining(clientNameRequest[2]), "Поле отчества должно заполниться данными из заявки");
		}


		/// <summary>
		/// В тесте при регистрации клиента поля из заявки в форме регистрации должны заполниться автоматически - правильность и заполнение этого проверяем в тесте
		/// Адрес в форме регистрации заполняется только при наличии полей Яндекса
		/// Так же проверяется, что регистрация клиента из заявки проходит успешно и в базе данных все сохраняется корректно
		/// </summary>
		[Test, Description("Регистрация клиента по заявке с полями адреса заполненные Яндекс")]
		public void ClientRequestRegistrationSuccessfully()
		{
			Setup();
			Open("Client/RequestsList");
			var requestRegistration = browser.FindElementByXPath("//td[contains(.,'" + "Сидоров" + "')]");
			var row = requestRegistration.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-green"));
			button.Click();
			AssertRequestData();

			//проверяем,что поле Город заполнилось данными из заявки
			var city = browser.FindElementsByCssSelector("select[id=RegionDropDown] option[selected]");
			var clientCity = city.Last().Text;
			Assert.That(clientCity, Is.StringContaining(clientRequest.City), "Поле Город должно заполниться данными из заявки");

			//проверяем,что поле Улица заполнилось данными из заявки 
			var street = browser.FindElementsByCssSelector("select[id=StreetDropDown] option[selected]");
			var clientStreet = street.Last().Text;
			Assert.That(clientStreet, Is.StringContaining(clientRequest.YandexStreet), "Поле Улица должно заполниться данными из заявки");

			//проверяем,что поле Дом заполнилось данными из заявки 
			var house = browser.FindElementsByCssSelector("select[id=HouseDropDown] option[selected]");
			var clientHouse = house.Last().Text;
			Assert.That(clientHouse, Is.StringContaining(clientRequest.YandexHouse), "Поле Дом должно заполниться данными из заявки");

			//проверяем,что поле Тариф заполнилось данными из заявки
			var tariff = browser.FindElementsByCssSelector("select[id=PlanDropDown] option[selected]");
			var clientPlan = tariff.Last().Text;
			Assert.That(clientPlan, Is.StringContaining(clientRequest.Plan.Name), "Поле Тарифный план должно заполниться данными из заявки");

			//проверяем,что в поле Привел клиента в компанию по умолчанию указан клиент
			var employee = browser.FindElementByCssSelector("select[id=EmployeeDropDown] option[selected]").Text;
			Assert.That(employee, Is.StringContaining("Клиент"), "Поле Привел клиента в компанию должно по умолчанию иметь значение Клиент");

			//дозаполняем поля, которых нет в клиентской заявке
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_CertificateName]").SendKeys("паспорт");
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_PassportSeries]").SendKeys("1234");
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_PassportNumber]").SendKeys("12345");
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_PassportDate]").SendKeys("09.01.1991");
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_PassportResidention]").SendKeys("Москва");
			browser.FindElementByCssSelector("input[id=client_PhysicalClient_RegistrationAddress]").SendKeys("Москва");
			browser.FindElementByCssSelector(".btn-green.save").Click();
			Open("Client/RequestsList");
			//проверяем, что клиент зарегестрирован
			var clientRegistration = DbSession.Query<PhysicalClient>().FirstOrDefault(p => p.Client._Name == "Сидоров Иван Андреевич");
			Assert.That(clientRegistration, Is.Not.Null, "В базе данных должен сохраниться зарегестрированный клиент");		
		}

		/// <summary>
		/// Создаем клиентскую заявку, где поля заполняемые Яндексом(улица и дом) пустые, адрес написанный клиентом написан с опечаткой
		/// В форме регистрации поля адреса не заполняются, сотрудник регестрирующий клиента заполняет это поле сам
		/// В тесте при регистрации клиента поля из заявки в форме регистрации должны заполниться автоматически, кроме адреса.
		/// </summary>
		[Test, Description("Регистрация клиента по заявке без полей адреса заполненные Яндексом и опечаткой в адресе внесенным клиентом")]
		public void ClientRequestRegistrationFails()
		{
			Setup();
			request.Street = "улица Гопчагарина";
			request.YandexStreet = "";
			request.YandexHouse = "";
			DbSession.Flush();
			DbSession.Save(request);

			Open("Client/RequestsList");
			var requestRegistration = browser.FindElementByXPath("//td[contains(.,'" + "Сидоров" + "')]");
			var row = requestRegistration.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-green"));
			button.Click();
			AssertRequestData();

			//проверяем,что поле Страница не заполнилось
			var city = browser.FindElementsByCssSelector("select[id=StreetDropDown] option[selected]");
			var clientCity = city.Last().Text;
			Assert.That(clientCity, Is.StringContaining(""), "Поле Улица в форме регетсрации  должно быть не заполненным");

			//проверяем,что поле Дом не заполнилось
			var house = browser.FindElementsByCssSelector("select[id=HouseDropDown] option[selected]");
			var clientHouse = house.Last().Text;
			Assert.That(clientHouse, Is.StringContaining(""), "Поле Дом в форме регетсрации  должно быть не заполненным");
		}

		/// <summary>
		/// Создаем клиентскую заявку, где поля заполняемые Яндексом(улица и дом) и поля заполненные клиентом(улица и дом) отличаются,но оба адреса есть в базе данных
		/// В этом случае приоритетным будет адрес клиента
		/// В форме регистрации поля адреса заполняются данными адреса, который ввел клиент
		/// </summary>
		[Test, Description("Регистрация клиента по заявке с полями адреса заполненные Яндекс и клиентом, но отличающимися друг от друга")]
		public void ClientPriorityRequestRegistration()
		{
			Setup();
			//данные адреса, которые ввел клиент
			request.City = "Борисоглебск";
			request.Street = "улица гагарина";
			request.HouseNumber = 1;
			request.Housing = "А";
			//данные адреса заполненные Яндексом
			request.YandexStreet = "улица пешкова";
			request.YandexHouse = "59";
			DbSession.Flush();
			DbSession.Save(request);

			Open("Client/RequestsList");
			var requestRegistration = browser.FindElementByXPath("//td[contains(.,'" + "Сидоров" + "')]");
			var row = requestRegistration.FindElement(By.XPath(".."));
			var button = row.FindElement(By.CssSelector("a.btn-green"));
			button.Click();
			AssertRequestData();

			//проверяем,что поле Страница заполнилось улицей вводимой клиентом
			var city = browser.FindElementsByCssSelector("select[id=StreetDropDown] option[selected]");
			var clientCity = city.Last().Text;
			Assert.That(clientCity, Is.StringContaining(request.Street), "Поле Улица в форме регетсрации должно быть заполнено улицей вводимой клиентом");

			//проверяем,что поле Страница заполнилось улицей вводимой клиентом
			var house = browser.FindElementsByCssSelector("select[id=HouseDropDown] option[selected]");
			var clientHouse = house.Last().Text;
			Assert.That(clientHouse, Is.StringContaining(request.HouseNumber + request.Housing), "Поле Дом в форме регетсрации  должно быть заполнено улицей вводимой клиентом");
		}

		[Test, Description("Добавление маркера")]
		public void ClientConnectionRequestMarker()
		{
			Open("Client/RequestsList");
			var panelCss = "#RequestMarkerColorChange ";
			var markerName = "Черный маркер";
			var markerColor = "#111111";
			var markerColorDifference = "#222222";


			var hasMarker = DbSession.Query<ConnectionRequestMarker>().FirstOrDefault(s => s.Name == markerName);
            Assert.That(hasMarker, Is.Null, "Маркера не должно быть в базе.");

			browser.FindElementByCssSelector("[data-target='#RequestMarkerColorChange']").Click();
			WaitForVisibleCss(panelCss + "input[name='name']", 20);

			var inputObj2 = browser.FindElementByCssSelector(panelCss + "input[name='color']");
			inputObj2.Clear();
			inputObj2.SendKeys(markerColor);
			browser.FindElementByCssSelector("#myModalLabel").Click();

			var inputObj = browser.FindElementByCssSelector(panelCss + ".modal-body input[name='name']");
			inputObj.Clear();
			inputObj.SendKeys(markerName);
			browser.FindElementByCssSelector("#myModalLabel").Click();
			 
			browser.FindElementByCssSelector("[onclick='addMarker(this)']").Click();
			WaitForVisibleCss("[name='markerList'] option", 20);
			DbSession.Flush();

			hasMarker = DbSession.Query<ConnectionRequestMarker>().FirstOrDefault(s => s.Name == markerName);
			Assert.That(hasMarker, Is.Not.Null, "Маркер должен быть в базе.");
			WaitForVisibleCss("[name='markerList'] option[value='" + hasMarker.Id + "']", 20);
			WaitForVisibleCss("[name='markerList'] option[color='" + hasMarker.Color + "']", 20);
			//WaitAjax(20);
			//AssertText("Список обновлен");

			inputObj = browser.FindElementByCssSelector(panelCss + "input[name='color']");
			Assert.That(inputObj.GetAttribute("value").IndexOf(markerColorDifference) == -1, Is.True, "Цвет не совпадает.");
			inputObj.Clear();
			inputObj.SendKeys(markerColorDifference);

			browser.FindElementByCssSelector("#myModalLabel").Click();
			browser.FindElementByCssSelector("[onclick='updateMarker(this)']").Click();
			WaitForVisibleCss("[name='markerList'] option[color='" + markerColorDifference + "']", 20);
			DbSession.Flush();
			DbSession.Refresh(hasMarker);
            Assert.That(hasMarker, Is.Not.Null, "Маркер должен быть в базе.");
			WaitForVisibleCss("[name='markerList'] option[value='" + hasMarker.Id + "']", 20);
			browser.FindElementByCssSelector("#exitButton").Click();

		}

	}
}