using System;
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
	internal class ServiceRequestFixture : ClientActionsFixture
	{
		/// <summary>
		/// Клиент с сервисной заявкой
		/// </summary>
		private Client ClientWithRequest { get; set; }

		/// <summary>
		///  Формирование сервисной заявки, выполнение стартовых условий
		/// </summary>
		/// <param name="createRequest">Создание новой заявки (по умолчанию)</param>
		/// <param name="requestPhone">Телефон в запросе</param>
		/// <param name="requestText">Текст запроса</param>
		/// <returns>Новая сервисная заявка (null, если флаг 'createRequest' = false)</returns>
		public ServiceRequest ServiceRequestFixtureStart(bool createRequest = true, string requestPhone = "123-4567890", string requestText = "Хочу интернета!")
		{
			//получаем клиента со статусом "нормальный клиент"
			ClientWithRequest = DbSession.Query<Client>().FirstOrDefault(s => s.Comment == "нормальный клиент");

			if (createRequest)
			{
				//удаление похожих сервисных заявок от клиента
				var listOfServiceRequestsToDelete = DbSession.Query<ServiceRequest>()
					.Where(s => s.Client == ClientWithRequest && s.Description == requestText && s.Phone == requestPhone).ToList();
				DbSession.DeleteEach(listOfServiceRequestsToDelete);

				//добавление сервисной заявки
				var serviceRequestToAdd = new ServiceRequest(ClientWithRequest);
				serviceRequestToAdd.Employee = Employee;
				serviceRequestToAdd.Phone = requestPhone;
				serviceRequestToAdd.Description = requestText;
				DbSession.Save(serviceRequestToAdd);
				return serviceRequestToAdd;
			}
			return null;
		}

		[Test, Description("Успешное создание сервисной заявки")]
		public void ServiceRequestCreateSuccessfully()
		{
			//выполнение стартовых условий
			const string requestText = "Хочу интернета!";
			const string requestPhone = "123-4567890";
			const string textSuccess = "Сервисная заявка успешно создана.";
			ServiceRequestFixtureStart(false);

			//переход на страницу: создания новой заявки
			Open("ServiceRequest/ServiceRequestCreate/" + ClientWithRequest.Id);
			//проверка соответствия: имени клиента
			AssertText(ClientWithRequest.PhysicalClient.FullName);
			//проверка соответствия: адреса клиента
			AssertText(ClientWithRequest.GetAddress());

			//ввод поля: Телефон для связи
			var serviceRequestPhone = browser.FindElementByName("serviceRequest.Phone");
			serviceRequestPhone.SendKeys(requestPhone);
			//ввод поля: Восстановление работы
			browser.FindElementByName("serviceRequest.BlockClientAndWriteOffs").Click();
			//ввод поля: Текст
			var serviceRequestText = browser.FindElementByName("serviceRequest.Description");
			serviceRequestText.SendKeys(requestText);
			//наджатие: кнопки подтверждения
			browser.FindElementByCssSelector("button.btn-green").Click();

			//Проверка на формирование сообщений
			var appealsToCheck = DbSession.Query<ServiceRequestComment>().Where(s => s.ServiceRequest.Client == ClientWithRequest).ToList().Where(d => d.Comment.IndexOf("<strong>необходимо восстановление работы</strong>") != -1).ToList();
			Assert.That(appealsToCheck.Count, Is.Not.EqualTo(0), "У клиента должны появиться аппилы после создания заявки.");

			//проверка соответствия: сообщения об успешном добавлении
			AssertText(textSuccess);
			//получение: только что созданной сервисной заявки
			var justCreatedServiceRequest = DbSession.Query<ServiceRequest>()
				.FirstOrDefault(s => s.Client == ClientWithRequest && s.Description == requestText && s.Phone == requestPhone);
			//проверка соответствия: только что созданной сервисной заявки - наличие
			Assert.That(justCreatedServiceRequest, Is.Not.Null, "Сервисная заявка не была добавлена!");
		}

		//TODO: вероятно нужно расширить тест
		[Test, Description("Неуспешное создание сервисной заявки")]
		public void ServiceRequestAddNosuccessfully()
		{
			//выполнение стартовых условий
			const string requestText = "Хочу интернета!";
			const string requestPhone = "123-4567890";
			const string textSuccess = "Сервисная заявка успешно создана.";
			ServiceRequestFixtureStart(false);

			//переход на страницу: редактирования заявки
			Open("ServiceRequest/ServiceRequestCreate/" + ClientWithRequest.Id);
			//проверка соответствия: имени клиента
			AssertText(ClientWithRequest.PhysicalClient.FullName);
			//проверка соответствия: адреса клиента
			AssertText(ClientWithRequest.GetAddress());
			//наджатие: кнопки подтверждения изменения заявки
			browser.FindElementByCssSelector("button.btn-green").Click();

			//проверка соответствия: сообщения об успешном добавлении
			AssertNoText(textSuccess);
			//получение: только что созданной сервисной заявки
			var justCreatedServiceRequest = DbSession.Query<ServiceRequest>()
				.FirstOrDefault(s => s.Client == ClientWithRequest && s.Description == requestText && s.Phone == requestPhone);
			//проверка соответствия: только что созданной сервисной заявки - отсуствие
			Assert.That(justCreatedServiceRequest, Is.Null, "Сервисная заявка не была добавлена!");
		}

		[Test, Description("Проверка изменений у клиента при смене статуса заявки - закрыта")]
		public void ServiceRequestEditStatusClosedForSum()
		{
			//выполнение стартовых условий
			const int writeOffSum = 197;
			var listOfUserWriteOffsToDelete = DbSession.Query<UserWriteOff>().Where(s => s.Sum == writeOffSum).ToList();
			DbSession.DeleteEach(listOfUserWriteOffsToDelete);
			var newServiceRequest = ServiceRequestFixtureStart();

			//переход на страницу редактирования заявки
			Open("ServiceRequest/ServiceRequestEdit/" + newServiceRequest.Id);
			//ввод поля: стоимость обслуживания
			var serviceRequestSum = browser.FindElementByName("serviceRequest.Sum");
			serviceRequestSum.SendKeys(writeOffSum.ToString());
			//ввод поля: статус - заявка закрыта
			Css("#serviceRequest_Status").SelectByText("Закрыта");
			//наджатие: кнопки подтверждения изменения заявки
			browser.FindElementByCssSelector("#ChangeServiceRequest").Click();

			//Проверка на формирование сообщений
			var appealsToCheck = DbSession.Query<ServiceRequestComment>().Where(s => s.ServiceRequest.Client == ClientWithRequest).ToList().Where(d => d.Comment.IndexOf("<strong>закрыта</strong>") != -1).ToList();
			Assert.That(appealsToCheck.Count, Is.Not.EqualTo(0), "У клиента должны появиться аппилы после закрытия заявки.");

			//проверка соответствия: успешное изменения заявки 
			AssertText("Сервисная заявка успешно обновлена.");
			//обновление модели: клиент
			DbSession.Refresh(ClientWithRequest);
			//проверка соответствия: статуса Пользователя
			Assert.That(ClientWithRequest.Status.Type, Is.EqualTo(StatusType.Worked), "Сервисная заявка не была изменена!");
			//проверка соответствия: наличия сформированного списания за обслуживания по заявке
			var userWriteOff = DbSession.Query<UserWriteOff>().FirstOrDefault(s => s.Sum == writeOffSum);
			Assert.That(userWriteOff, Is.Not.Null, "При закрытии заявки необходимо сформировать сумму списания.");
			Assert.That(userWriteOff.Date, Is.Not.Null, "Дата списания должна быть указана.");
		}

		[Test, Description("Проверка изменений у клиента при смене статуса бесплатной заявки - закрыта")]
		public void ServiceRequestEditStatusClosedForFree()
		{
			//выполнение стартовых условий
			const int writeOffSum = 197;
			const string requestFreeReason = "гладиолус.";
			var listOfUserWriteOffsToDelete = DbSession.Query<UserWriteOff>().Where(s => s.Sum == writeOffSum).ToList();
			DbSession.DeleteEach(listOfUserWriteOffsToDelete);
			var newServiceRequest = ServiceRequestFixtureStart();

			//переход на страницу редактирования сервисной заявки
			Open("ServiceRequest/ServiceRequestEdit/" + newServiceRequest.Id);
			//ввод поля: стоимость обслуживания
			var serviceRequestSum = browser.FindElementByName("serviceRequest.Sum");
			serviceRequestSum.SendKeys(writeOffSum.ToString());
			//наджатие: кнопки 'Бесплатная сервисная заявка'
			browser.FindElementByName("serviceRequest.Free").Click();
			//ввод поля: Причина бесплатной сервисной заявки
			var serviceRequestFreeReason = browser.FindElementByName("reasonForFree.Comment");
			serviceRequestFreeReason.SendKeys(requestFreeReason);
			//ввод поля: статус - заявка закрыта
			Css("#serviceRequest_Status").SelectByText("Закрыта");
			//наджатие: кнопки подтверждения изменения заявки
			browser.FindElementByCssSelector("#ChangeServiceRequest").Click();

			//проверка соответствия: успешное изменения 
			AssertText("Сервисная заявка успешно обновлена.");
			//обновление модели: клиент
			DbSession.Refresh(ClientWithRequest);

			//проверка соответствия: статуса Пользователя
			Assert.That(ClientWithRequest.Status.Type, Is.EqualTo(StatusType.Worked), "Сервисная заявка была закрыта с ошибкой!");
			//проверка соответствия: наличия сформированного списания за обслуживания по заявке
			var userWriteOff = DbSession.Query<UserWriteOff>().FirstOrDefault(s => s.Sum == writeOffSum);
			var userWriteOffAppeal = DbSession.Query<ServiceRequestComment>().Where(s => s.ServiceRequest.Client == ClientWithRequest).ToList().FirstOrDefault(d => d.Comment.IndexOf("<strong>закрыта</strong>") != -1);
			Assert.That(userWriteOff, Is.Null, "При закрытии бесплатной заявки была сформирована сумма списания.");
			Assert.That(userWriteOffAppeal.CreationDate, Is.Not.Null, "Дата списания должна быть указана.");
		}


		[Test, Description("Проверка изменений у клиента при смене статуса заявки - отменена")]
		public void ServiceRequestEditStatusCancel()
		{
			//выполнение стартовых условий
			var newServiceRequest = ServiceRequestFixtureStart();

			//переход на страницу редактирования сервисной заявки
			Open("ServiceRequest/ServiceRequestEdit/" + newServiceRequest.Id);
			//ввод поля: статус - заявка отменена
			Css("#serviceRequest_Status").SelectByText("Отменена");
			//наджатие: кнопки подтверждения изменения заявки
			browser.FindElementByCssSelector("#ChangeServiceRequest").Click();

			//Проверка на формирование сообщений
			var appealsToCheck = DbSession.Query<ServiceRequestComment>().Where(s => s.ServiceRequest.Client == ClientWithRequest).ToList().Where(d => d.Comment.IndexOf("<strong>отменена</strong>") != -1).ToList();
			Assert.That(appealsToCheck.Count, Is.Not.EqualTo(0), "У клиента должны появиться аппилы после отмены заявки.");


			//проверка соответствия: успешное изменения 
			AssertText("Сервисная заявка успешно обновлена.");
			//обновление модели: клиент
			DbSession.Refresh(ClientWithRequest);
			//проверка соответствия: статуса Пользователя
			Assert.That(ClientWithRequest.Status.Type, Is.EqualTo(StatusType.Worked), "Сервисная заявка была отменена с ошибкой!");
		}

		[Test, Description("Проверка изменений у клиента при статусе заявки - Восстановление работы")]
		public void ServiceRequestEditClientBlock()
		{
			//выполнение стартовых условий
			var newServiceRequest = ServiceRequestFixtureStart();

			Open("ServiceRequest/ServiceRequestEdit/" + newServiceRequest.Id);
			//наджатие: кнопки, выставляющей пользователю статус 'BlockedForRepair'
			browser.FindElementByLinkText("Восстановление работы").Click();


			//Проверка на формирование сообщений
			var appealsToCheck = DbSession.Query<ServiceRequestComment>().Where(s => s.ServiceRequest.Client == ClientWithRequest).ToList().Where(d => d.Comment.IndexOf("<strong>необходимо восстановление работы</strong>") != -1).ToList();
			Assert.That(appealsToCheck.Count, Is.Not.EqualTo(0), "У клиента должны появиться аппилы после нажатие на кнопку 'восстановление работы'.");

			//обновление модели: клиент
			DbSession.Refresh(ClientWithRequest);
			//проверка соответствия: статуса Пользователя
			Assert.That(ClientWithRequest.Status.Type, Is.EqualTo(StatusType.BlockedForRepair), "Сервисная заявка не была изменена!");

			//переход на страницу редактирования сервисной заявки
			Open("ServiceRequest/ServiceRequestEdit/" + newServiceRequest.Id);
			//ввод поля: статус - заявка закрыта
			Css("#serviceRequest_Status").SelectByText("Закрыта");
			//наджатие: кнопки подтверждения изменения заявки
			browser.FindElementByCssSelector("#ChangeServiceRequest").Click();

			//обновление модели: клиент
			DbSession.Refresh(ClientWithRequest);
			//проверка соответствия: статуса Пользователя
			Assert.That(ClientWithRequest.Status.Type, Is.EqualTo(StatusType.Worked), "При закрытии сервисной заявки статус не изменился!");
		}
	}
}