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
			var mailhelper = new Mailer();
			var clientWithEmptyHouseObj = session.Query<Client>().FirstOrDefault(s => s.PhysicalClient != null);
			if (clientWithEmptyHouseObj != null) {
				clientWithEmptyHouseObj.PhysicalClient.HouseObj = null;
				clientWithEmptyHouseObj.Status.Id = 5;
				session.Update(clientWithEmptyHouseObj);
				session.Flush();
			}
			string mailAboutHouseObjAbsence = new DataAudit(session).CheckForHouseObjAbsence();
			Assert.That(mailAboutHouseObjAbsence, Is.Not.EqualTo(""));
		}

		[Test(Description = "Проверка на формирование сообщения при отсуствии физика с пустым полем HouseObj.")]
		public void CheckForHouseObjAbsenceFixtureExistsNot()
		{
			var clientWithEmptyHouseObj = session.Query<Client>().Where(s => s.PhysicalClient != null && s.PhysicalClient.HouseObj == null
			                                                                 && s.Status.Id != 10 && s.Status.Id != 3 && s.Status.Id != 1).ToList();
			var houseObj = session.Query<House>().FirstOrDefault();
			foreach (var item in clientWithEmptyHouseObj) {
				item.PhysicalClient.HouseObj = houseObj;
				item.Status.Id = 5;
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
			var settings = session.Query<InternetSettings>().First();

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