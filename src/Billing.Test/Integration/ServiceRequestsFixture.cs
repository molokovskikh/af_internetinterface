using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	public class ServiceRequestsFixture : MainBillingFixture
	{
		// Проверяет, что время time приближено к текущему времени (оценочно в интервале 5 сек.)
		private bool IsApproachedToNow(DateTime? time)
		{
			if (!time.HasValue || time.Value.Date != SystemTime.Today())
				return false;
			if (SystemTime.Now().TimeOfDay - time.Value.TimeOfDay < TimeSpan.FromSeconds(5))
				return true;
			return false;
		}

		[TestCase(arg: "Борисоглебск", Description = "Проверка обработки истекшей сервисной заявки")]
		public void ProcessPastServiceRequest(string cityName)
		{
			using (new SessionScope()) {
				// Изменить город клиента на cityName
				var region = client.GetRegion();
				if (region == null || region.City == null || region.City.Name != cityName) {
					var thisRegion = ActiveRecordMediator<RegionHouse>.FindAllByProperty("Name", cityName).FirstOrDefault();
					client.PhysicalClient.HouseObj = new House("улица", 1, thisRegion);

					var endpoint = new ClientEndpoint() { IsEnabled = true, Client = client };
					client.Endpoints.Add(endpoint);
					ActiveRecordMediator.Save(endpoint);
					ActiveRecordMediator.Save(client.PhysicalClient.HouseObj);
					ActiveRecordMediator.Update(client.PhysicalClient);
					ActiveRecordMediator.Update(client);
				}

				// Создать в БД новую сервисную заявку
				var serviceRequest = new ServiceRequest {
					Client = client,
					Description = "Тестовая заявка",
					RegDate = SystemTime.Now(),
					Status = ServiceRequestStatus.New,
					ModificationDate = SystemTime.Now(),
					Sum = 100m
				};
				ActiveRecordMediator.SaveAndFlush(serviceRequest);
				ActiveRecordMediator.Refresh(serviceRequest);
				client.Refresh();
				Assert.IsTrue(client.ServiceRequests.Count == 1, "\nНе создалась сервисная заявка");

				// Получить/создать пользователя "redmine", от лица которого Billing создает заметку в RedMine
				var redmineUser = ActiveRecordMediator<RedmineUser>.FindAllByProperty("Login", "redmine").FirstOrDefault();
				if (redmineUser == null) {
					redmineUser = new RedmineUser {
						Login = "redmine",
						FirstName = "Redmine",
						LastName = "Система",
						Email = "redmine@analit.net"
					};
					ActiveRecordMediator.Save(redmineUser);
					redmineUser.Refresh();
				}

				// Сдвинуть текущую дату на 2 дня и убедиться, что Billing не нашел истекших сервисных заявок
				SystemTime.Now = () => DateTime.Now.AddDays(2);
				billing.ProcessWriteoffs();
				var note = ActiveRecordMediator<RedmineJournal>.FindAllByProperty("CreateDate", "UserId", redmineUser.Id).LastOrDefault();
				Assert.IsTrue(note == null || !IsApproachedToNow(note.CreateDate), "\nИмеется заметка в RedMine");

				// Сдвинуть текущую дату на 3 дня и убедиться, что Billing обработал истекшую сервисную заявку
				SystemTime.Now = () => DateTime.Now.AddDays(3);
				billing.ProcessWriteoffs();
				note = ActiveRecordMediator<RedmineJournal>.FindAllByProperty("CreateDate", "UserId", redmineUser.Id).LastOrDefault();
				Assert.IsTrue(note != null && note.Notes.Contains("Срок исполнения сервисной") && IsApproachedToNow(note.CreateDate));
			}
		}

		[Test(Description = "Проверка обработки истекшей сервисной заявки для безымянного города")]
		public void ProcessPastServiceRequestForNonameCity()
		{
			ProcessPastServiceRequest("NonameCity");
		}
	}
}
