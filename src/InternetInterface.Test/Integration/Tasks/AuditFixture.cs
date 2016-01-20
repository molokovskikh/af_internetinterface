using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Background;
using InternetInterface.Models;
using InternetInterface.Services;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NHibernate.Util;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration.Tasks
{
	[TestFixture]
	public class AuditFixture : IntegrationFixture
	{
		[Test(Description = "Проверка добавления физикам отсутствующего HouseObj.")]
		public void CheckForHouseObjAbsenceFixtureExists()
		{
			//ТЕСТИРОВАТЬ НУЖНО, СОЗДАВАЯ КЛИЕНТА В НОВЫМИ ТЕСТАМИ ~~~
			// Создаем необходимые данные 
			if (session.Query<Status>().Count() < 5) {
				for (int i = 0; i < 10; i++) session.Save(new Status() {Name = "ssdsd" + i, ShortName = "sdsd"});
				session.Flush();
			}
			var clientWithEmptyHouseObj = new Client
			{
				PhysicalClient = new PhysicalClient
				{
					Password = "sdcsdcsdcsdcsdscfv",
					PhoneNumber = "4951234567",
					Email = "test@client.ru",
					Name = "Иван",
					Surname = "Кузнецов",
					Patronymic = "нормальный клиент",
					PassportDate = DateTime.Now.AddYears(-20),
					DateOfBirth = DateTime.Now.AddYears(-40),
					PassportNumber = "123456",
					PassportSeries = "1234",
					Balance = 1000
				},
				SendSmsNotification = false,
				Disabled = false,
				RatedPeriodDate = DateTime.Now,
				FreeBlockDays = 28
			};
			clientWithEmptyHouseObj.PhysicalClient.HouseObj = null;
			clientWithEmptyHouseObj.Status = session.Query<Status>().FirstOrDefault(s => s.Id == 5);
			session.Save(clientWithEmptyHouseObj);
			session.Flush();

			session.CreateSQLQuery(
				string.Format(@"INSERT INTO internet.inforoom2_city(Name) VALUES({0});", "'Городок'")).UniqueResult();
			var LastItemInforoom2_city =
				session.CreateSQLQuery(string.Format(@"SELECT Id FROM internet.inforoom2_city order by Id DESC  limit 1;")).UniqueResult();
			
			session.CreateSQLQuery(
				string.Format(@"INSERT INTO internet.regions(Region,_City) VALUES({0},{1});", "'ПодГородок'", LastItemInforoom2_city))
				.UniqueResult();
			var LastItemRegion =
				session.CreateSQLQuery(string.Format(@"SELECT Id FROM internet.regions order by Id DESC  limit 1;")).UniqueResult();

			session.CreateSQLQuery(
				string.Format(@"INSERT INTO internet.inforoom2_street(Name,Region) VALUES({0},{1});", "'Первомайская'", LastItemRegion))
				.UniqueResult();
			var LastItemInforoom2_street =
				session.CreateSQLQuery(string.Format(@"SELECT Id FROM internet.inforoom2_street order by Id DESC  limit 1;")).UniqueResult();

			session.CreateSQLQuery(
				string.Format(@"INSERT INTO internet.inforoom2_house(Number,Street) VALUES({0},{1});", "'13'", LastItemInforoom2_street))
				.UniqueResult();
			var LastItemInforoom2_house =
				session.CreateSQLQuery(string.Format(@"SELECT Id FROM internet.inforoom2_house order by Id DESC  limit 1;")).UniqueResult();

			session.CreateSQLQuery(
				string.Format(@"INSERT INTO internet.inforoom2_address(house,Floor,Apartment,Entrance) VALUES({0},{1},{2},{3});", LastItemInforoom2_house,3,"'4'","'1'")).UniqueResult();
			var LastItemInforoom2_address =
				session.CreateSQLQuery(string.Format(@"SELECT Id FROM internet.inforoom2_address order by Id DESC  limit 1;")).UniqueResult();

			session.CreateSQLQuery(
				string.Format(@"UPDATE internet.physicalclients AS p SET p._Address = {1} WHERE p.Id = {0};", clientWithEmptyHouseObj.PhysicalClient.Id, LastItemInforoom2_address)).UniqueResult();

			

			// проводим тестирвоание
			var dataAudit = new DataAudit(session);
			dataAudit.CheckForHouseObjAbsence();

			session.Refresh(clientWithEmptyHouseObj.PhysicalClient);
			Assert.That(clientWithEmptyHouseObj.PhysicalClient.HouseObj, Is.Not.Null);
		}

		[Test(Description = "Проверка на формирование сообщения при наличии подозрительного клиента.")]
		public void CheckForSuspiciousClientFixtureExists()
		{
			// Создаем необходимые данные 
			var settings = session.Query<InternetSettings>().First();
			if (session.Query<Status>().Count() < 5) {
				for (int i = 0; i < 10; i++) session.Save(new Status() {Name = "ssdsd" + i, ShortName = "sdsd"});
				session.Flush();
			}
			if (settings == null) {
				settings = new InternetSettings() {NextBillingDate = DateTime.Now};
				session.Save(settings);
			}
			var status = session.Load<Status>((uint) StatusType.Worked);
			var clientSuspiciousClient = session.Query<Client>().FirstOrDefault();
			if (clientSuspiciousClient != null) {
				clientSuspiciousClient.Status = status;
				clientSuspiciousClient.Disabled = false;
				clientSuspiciousClient.RatedPeriodDate = null;

				session.Update(clientSuspiciousClient);
				session.Flush();
			}
			var dataAudit = new DataAudit(session);
			dataAudit.CheckForSuspiciousClient();
			Assert.That(dataAudit.Reports.Count, Is.EqualTo(1));
		}

		[Test(Description = "Проверка на формирование сообщения при отсуствии подозрительного клиента.")]
		public void CheckForSuspiciousClientFixtureExistsNot()
		{
			var status = session.Load<Status>((uint) StatusType.Worked);
			var clientSuspiciousClients =
				session.Query<Client>()
					.Where(i => i.PhysicalClient != null && i.Disabled == false && i.Status == status && i.RatedPeriodDate == null)
					.ToList();
			status = session.Load<Status>((uint) StatusType.BlockedAndNoConnected);
			foreach (var item in clientSuspiciousClients) {
				item.Status = status;
				item.Disabled = true;
				session.Update(item);
			}
			session.Flush();
			var dataAudit = new DataAudit(session);
			dataAudit.CheckForSuspiciousClient();
			Assert.That(dataAudit.Reports.Count, Is.EqualTo(0));
		}

		[Test(Description = "Проверка на нежелательную блокировку услуги обещанный платеж к задаче")]
		public void CheckForDefferedPayment()
		{
			//Убираем из выборки все старые сервисы
			var oldServicers = session.Query<ClientService>().ToList();
			oldServicers.Each(i => i.EndWorkDate = SystemTime.Now().AddDays(-10));
			oldServicers.Each(i => session.Save(i));

			//Создаем клиента и подключаем ему сервис
			var client = ClientHelper.Client(session);
			client.WriteOff(10000, false);
			client.SetStatus(StatusType.NoWorked, session);
			session.Save(client);
			var service = session.Query<Service>().First(i => i.HumanName.Contains("Обещанный"));
			var clientservice = new ClientService(client, service, true);
			clientservice.EndWorkDate = SystemTime.Now().AddDays(3);
			clientservice.TryActivate();
			session.Save(client);
			session.Save(clientservice);
			session.Flush();

			//Тест
			var audit = new DataAudit(session);
			audit.CheckForDefferedPaymentFailure();
			//Пока все нормально
			Assert.That(audit.Reports.Count, Is.EqualTo(0));

			//Создаем ненормальную ситуацию
			client.SetStatus(StatusType.NoWorked, session);
			session.Save(client);
			session.Flush();
			audit.CheckForDefferedPaymentFailure();
			Assert.That(audit.Reports.Count, Is.EqualTo(1));
			Assert.That(audit.Reports[audit.Reports.Keys[0]], Is.StringContaining(client.Id.ToString()));
		}
	}
}