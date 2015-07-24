using System;
using System.Collections.Generic;
using System.Linq;
using Common.Tools.Calendar;
using Common.Web.Ui.NHibernateExtentions;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace InforoomControlPanel.Test.Functional.ClientActions
{
	internal class ConnectionTeamFixture : ClientActionsFixture
	{
		/// <summary>
		/// Клиент с сервисной заявкой
		/// </summary>
		private Client CurrentClient { get; set; }

		/// <summary>
		/// Получение нового Инженера, по заданным условиям (если таких нет, инженер создается)
		/// </summary>
		/// <param name="region">Регион</param>
		/// <param name="employee">Работник</param>
		/// <param name="createNew">Создание нового инженера (без проверки)</param>
		/// <returns>Инженер</returns>
		public ServiceMan GetServiceMan(Region region, Employee employee, bool createNew = false)
		{
			//получение существующего инженера
			var necessaryServiceMan = createNew ? null : DbSession.Query<ServiceMan>().FirstOrDefault(s => s.Employee == employee && s.Region == region);
			if (necessaryServiceMan == null) {
				//создание нового инженера
				var newServiceMan = new ServiceMan { Employee = employee, Region = region };
				DbSession.Save(newServiceMan);
				return newServiceMan;
			}
			return necessaryServiceMan;
		}

		/// <summary>
		///  Выполнение стартовых условий
		/// </summary>
		/// <param name="getFromServiceRequest">True, если 'запись в график' на основе сервисной заявки (Id севисной заявки), False, если на основе заявки на подлючение (Id клиента) </param>
		/// <param name="justGetId">True, если необходимо только получить Id</param>
		/// <returns>Id новой сервисной заявки (или имеющегося клиента)</returns>
		public int ConnectionTeamFixtureStart(bool getFromServiceRequest = true, bool justGetId = false)
		{
			if (!justGetId) {
				var serviceManToDelete = DbSession.Query<ServiceMan>().ToList();
				DbSession.DeleteEach(serviceManToDelete);

				//кол-во инженеров, которых необходимо создать для каждого региона из СУЩЕСТВУЮЩИХ работников
				const int numberOfServiceMenTocreate = 8;
				//индекс созданного инженера (из работников)
				int employeesListStartIndex = 0;
				var employeesList = DbSession.Query<Employee>().ToList();
				var regionsList = DbSession.Query<Region>().ToList();

				foreach (Region t in regionsList) {
					//для каждого региона создаем инженеров
					for (int j = 0; j < numberOfServiceMenTocreate; j++) GetServiceMan(t, employeesList[employeesListStartIndex++], true);
				}

				//получаем клиента со статусом "нормальный клиент"
				CurrentClient = DbSession.Query<Client>().FirstOrDefault(s => s.Comment == "нормальный клиент");
			}
			if (getFromServiceRequest) {
				//данные по сервисной заявке
				const string requestText = "Хочу интернета!";
				const string requestPhone = "123-4567890";

				//удаление похожих сервисных заявок от клиента
				var listOfServiceRequestsToDelete = DbSession.Query<ServiceRequest>()
					.Where(s => s.Client == CurrentClient && s.Description == requestText && s.Phone == requestPhone).ToList();
				DbSession.DeleteEach(listOfServiceRequestsToDelete);

				//добавление сервисной заявки
				var serviceRequestToAdd = new ServiceRequest(CurrentClient);
				serviceRequestToAdd.Employee = Employee;
				serviceRequestToAdd.Phone = requestPhone;
				serviceRequestToAdd.Description = requestText;
				DbSession.Save(serviceRequestToAdd);

				return serviceRequestToAdd.Id;
			}
			else {
				return CurrentClient.Id;
			}
		}

		[Test, Description("Успешное добавление записи в график инженеров из сервисной заявки")]
		public void ServicemenScheduleItemCreateSuccessfullyFromServiceRequest()
		{
			//выполнение стартовых условий 
			var serviceRequestId = ConnectionTeamFixtureStart();
			const string textSuccess = "Сервисная заявка успешно добавлена в график";
			Employee employee = DbSession.Query<Employee>().FirstOrDefault(s => s.Name == "Золотарев Александр Алексеевич");

			//переход на страницу: создания новой записи в график инженеров
			Open("ConnectionTeam/AttachRequest/" + serviceRequestId + "?type=ServiceRequest");
			//проверка соответствия: имени клиента
			AssertText(CurrentClient.PhysicalClient.FullName);
			//получение инженера по региону клиента и работнику
			var serviceMan = GetServiceMan(CurrentClient.Address.Region, employee);
			//нажатие на элемент графика инженеров (с необходимым временем)
			browser.FindElementsByCssSelector("td.employee" + serviceMan.Id + ".time").FirstOrDefault(s => s.Text == "10:30").Click();
			//наджатие: кнопки назначения в график
			browser.FindElementByCssSelector("#submitScheduleItem").Click();

			//проверка соответствия: сообщения об успешном добавлении
			AssertText(textSuccess);
			//получение: только что созданной записи в график инженеров
			var justCreatedServicemenScheduleItem = DbSession.Query<ServicemenScheduleItem>()
				.FirstOrDefault(s => s.ServiceRequest.Id == serviceRequestId && s.RequestType == ServicemenScheduleItem.Type.ServiceRequest);
			//проверка соответствия: только что созданной записи в график инженеров - наличие
			Assert.That(justCreatedServicemenScheduleItem, Is.Not.Null, "Запись в график инженеров не была добавлена!");
		}

		[Test, Description("Успешное добавление записи в график инженеров из заявки на подключение")]
		public void ServicemenScheduleItemCreateSuccessfullyFromClient()
		{
			//выполнение стартовых условий 
			var connectionRequestId = ConnectionTeamFixtureStart(false);
			const string textSuccess = "Заявка на подключение успешно добавлена в график";
			Employee employee = DbSession.Query<Employee>().FirstOrDefault(s => s.Name == "Золотарев Александр Алексеевич");

			//переход на страницу: создания новой записи в график инженеров
			Open("ConnectionTeam/AttachRequest/" + connectionRequestId + "?type=ClientConnectionRequest");
			//проверка соответствия: имени клиента
			AssertText(CurrentClient.PhysicalClient.FullName);
			//получение инженера по региону клиента и работнику
			var serviceMan = GetServiceMan(CurrentClient.Address.Region, employee);
			//нажатие на элемент графика инженеров (с необходимым временем)
			browser.FindElementsByCssSelector("td.employee" + serviceMan.Id + ".time").FirstOrDefault(s => s.Text == "10:30").Click();
			//наджатие: кнопки назначения в график
			browser.FindElementByCssSelector("#submitScheduleItem").Click();

			//проверка соответствия: сообщения об успешном добавлении
			AssertText(textSuccess);
			//получение: только что созданной записи в график инженеров
			var justCreatedServicemenScheduleItem = DbSession.Query<ServicemenScheduleItem>()
				.FirstOrDefault(s => s.Client.Id == connectionRequestId && s.RequestType == ServicemenScheduleItem.Type.ClientConnectionRequest);
			//проверка соответствия: только что созданной записи в график инженеров - отсуствие
			Assert.That(justCreatedServicemenScheduleItem, Is.Not.Null, "Запись в график инженеров не была добавлена!");
		}

		[Test, Description("Конфликт временных промежутков, при редактировании записи в графике инженеров")]
		public void ServicemenScheduleItemTimeConflict()
		{
			//выполнение стартовых условий 
			var serviceRequestId = ConnectionTeamFixtureStart();
			var connectionRequestId = ConnectionTeamFixtureStart(false, true);
			const string textSuccessServiceRequest = "Сервисная заявка успешно добавлена в график";
			const string textSuccessConnectionRequest = "Заявка на подключение успешно добавлена в график";
			const string textErrorConnectionRequest = "Назначенное время в графике исполнителя уже занято!";
			Employee employee = DbSession.Query<Employee>().FirstOrDefault(s => s.Name == "Золотарев Александр Алексеевич");

			//переход на страницу: редактировани записи в графике инженеров
			Open("ConnectionTeam/AttachRequest/" + serviceRequestId + "?type=ServiceRequest");
			//проверка соответствия: имени клиента
			AssertText(CurrentClient.PhysicalClient.FullName);
			//получение инженера по региону клиента и работнику
			var serviceMan = GetServiceMan(CurrentClient.Address.Region, employee);
			//нажатие на элемент графика инженеров (с необходимым временем)
			browser.FindElementsByCssSelector("td.employee" + serviceMan.Id + ".time").FirstOrDefault(s => s.Text == "10:30").Click();
			//наджатие: кнопки назначения в график
			browser.FindElementByCssSelector("#submitScheduleItem").Click();
			//проверка соответствия: сообщения об успешном добавлении
			AssertText(textSuccessServiceRequest);

			//переход на страницу: редактировани записи в графике инженеров
			Open("ConnectionTeam/AttachRequest/" + connectionRequestId + "?type=ClientConnectionRequest");
			//проверка соответствия: имени клиента
			AssertText(CurrentClient.PhysicalClient.FullName);
			//нажатие на элемент графика инженеров (с необходимым временем)
			browser.FindElementsByCssSelector("td.employee" + serviceMan.Id + ".time").FirstOrDefault(s => s.Text == "11:30").Click();
			//наджатие: кнопки назначения в график
			browser.FindElementByCssSelector("#submitScheduleItem").Click();
			//проверка соответствия: сообщения об успешном добавлении
			AssertText(textSuccessConnectionRequest);

			//переход на страницу: редактировани записи в графике инженеров
			Open("ConnectionTeam/AttachRequest/" + serviceRequestId + "?type=ServiceRequest");
			//увеличение длительности работ инженера
			browser.FindElementsByCssSelector("button").FirstOrDefault(s => s.Text == "+").Click();
			//наджатие: кнопки назначения в график
			browser.FindElementByCssSelector("#submitScheduleItem").Click();
			//проверка соответствия: сообщения об успешном добавлении
			AssertText(textErrorConnectionRequest);
		}
	}
}