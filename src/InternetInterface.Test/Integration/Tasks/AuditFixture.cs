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
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration.Tasks
{
	[TestFixture]
	public class AuditFixture : IntegrationFixture
	{
		[Test(Description = "Проверка на формирование сообщения при наличии физика с пустым полем HouseObj.")]
		public void CheckForHouseObjAbsenceFixtureExists()
		{
			// Создаем необходимые данные 
			var mailhelper = new Mailer();
			if (session.Query<Status>().Count() < 5) {
				for (int i = 0; i < 10; i++) session.Save(new Status() { Name = "ssdsd" + i, ShortName = "sdsd" });
				session.Flush();
			}
			var clientWithEmptyHouseObj = new Client {
				PhysicalClient = new PhysicalClient {
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
			// проводим тестирвоание
			string mailAboutHouseObjAbsence = new DataAudit(session).CheckForHouseObjAbsence();
			Assert.That(mailAboutHouseObjAbsence, Is.Not.EqualTo(""));
		}

		[Test(Description = "Проверка на формирование сообщения при отсуствии физика с пустым полем HouseObj.")]
		public void CheckForHouseObjAbsenceFixtureExistsNot()
		{
			var clientWithEmptyHouseObj = session.Query<Client>().Where(s => s.PhysicalClient != null && s.PhysicalClient.HouseObj == null && s.Status.Id != 10 && s.Status.Id != 3 && s.Status.Id != 1).ToList();
			var houseObj = session.Query<House>().FirstOrDefault();
			var statusFive = session.Query<Status>().FirstOrDefault(s => s.Id == 5);
			foreach (var item in clientWithEmptyHouseObj) {
				item.PhysicalClient.HouseObj = houseObj;
				item.Status = statusFive;
				session.Update(item);
			}
			session.Flush();
			string mailAboutHouseObjAbsence = new DataAudit(session).CheckForHouseObjAbsence();
			Assert.That(mailAboutHouseObjAbsence, Is.EqualTo(""));
		}

		[Test(Description = "Проверка на формирование сообщения при наличии подозрительного клиента.")]
		public void CheckForSuspiciousClientFixtureExists()
		{
			var mailhelper = new Mailer();
			// Создаем необходимые данные 
			var settings = session.Query<InternetSettings>().First();
			if (session.Query<Status>().Count() < 5) {
				for (int i = 0; i < 10; i++) session.Save(new Status() { Name = "ssdsd" + i, ShortName = "sdsd" });
				session.Flush();
			}
			if (settings == null) {
				settings = new InternetSettings() { NextBillingDate = DateTime.Now };
				session.Save(settings);
			}
			var status = session.Load<Status>((uint)StatusType.Worked);
			var clientSuspiciousClient = session.Query<Client>().FirstOrDefault();
			if (clientSuspiciousClient != null) {
				clientSuspiciousClient.Status = status;
				clientSuspiciousClient.Disabled = false;
				clientSuspiciousClient.RatedPeriodDate = null;

				session.Update(clientSuspiciousClient);
				session.Flush();
			}
			int suspiciousClientNumber = 0;
			// проводим тестирвоание
			string mailAboutSuspiciousClient = new DataAudit(session).CheckForSuspiciousClient(settings, ref suspiciousClientNumber);
			if (mailAboutSuspiciousClient != string.Empty) {
				mailhelper.SendText("service@analit.net", "service@analit.net", "Подозрительные клиенты в InternetInterface: " + suspiciousClientNumber, mailAboutSuspiciousClient);
			}
			Assert.That(mailAboutSuspiciousClient, Is.Not.EqualTo(""));
			Assert.That(suspiciousClientNumber, Is.Not.EqualTo(0));
		}

		[Test(Description = "Проверка на формирование сообщения при отсуствии подозрительного клиента.")]
		public void CheckForSuspiciousClientFixtureExistsNot()
		{
			var mailhelper = new Mailer();
			var settings = session.Query<InternetSettings>().First();
			var status = session.Load<Status>((uint)StatusType.Worked);
			var clientSuspiciousClients = session.Query<Client>().Where(i => i.PhysicalClient != null && i.Disabled == false && i.Status == status && i.RatedPeriodDate == null).ToList();
			status = session.Load<Status>((uint)StatusType.BlockedAndNoConnected);
			foreach (var item in clientSuspiciousClients) {
				item.Status = status;
				item.Disabled = true;
				session.Update(item);
			}
			session.Flush();
			int suspiciousClientNumber = 0;
			string mailAboutSuspiciousClient = new DataAudit(session).CheckForSuspiciousClient(settings, ref suspiciousClientNumber);
			if (mailAboutSuspiciousClient != string.Empty) {
				mailhelper.SendText("service@analit.net", "service@analit.net", "Подозрительные клиенты в InternetInterface: " + suspiciousClientNumber, mailAboutSuspiciousClient);
			}
			Assert.That(mailAboutSuspiciousClient, Is.EqualTo(""));
			Assert.That(suspiciousClientNumber, Is.EqualTo(0));
		}
	}
}