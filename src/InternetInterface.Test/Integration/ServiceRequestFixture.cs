﻿using System;
using System.IO;
using System.Linq;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class ServiceRequestFixture : IntegrationFixture
	{
		private ServiceRequest request;
		private Partner engineer1;

		[SetUp]
		public void Setup()
		{
			var client = ClientHelper.Client();
			session.Save(client);

			engineer1 = new Partner(Guid.NewGuid().ToString()) {
				TelNum = "980-8791258",
				Categorie = session.Query<UserCategorie>().First(c => c.ReductionName == "Service")
			};
			session.Save(engineer1);

			request = new ServiceRequest {
				Contact = "950-5001055",
				Client = client,
				Performer = engineer1
			};
			session.Save(request);
		}

		[TearDown]
		public void Teardown()
		{
			//хак что бы обойти ошибку TearDown : NHibernate.AssertionFailure : collection [InternetInterface.Models.Partner.Payments] was not processed by flush()
			session.Clear();
		}

		[Test]
		public void Sms_should_contains_perform_date_time()
		{
			request.PerformanceDate = new DateTime(2012, 05, 21, 17, 00, 00);
			var sms = request.GetNewSms();
			Assert.That(sms.Text, Is.StringStarting("$ 21.05.2012 17:00:00"));
		}

		[Test]
		public void On_close_request_make_write_off()
		{
			request.Sum = 200;
			request.Status = ServiceRequestStatus.Close;
			var writeOff = request.GetWriteOff(session);
			Assert.That(writeOff, Is.Not.Null);
			Assert.That(writeOff.Sum, Is.EqualTo(200));
			Assert.That(writeOff.Comment, Is.StringContaining("Оказание доп"));
		}

		[Test]
		public void Send_cancelation_on_performer_change()
		{
			var engineer2 = new Partner(Guid.NewGuid().ToString()) {
				TelNum = "920-1564189",
				Categorie = session.Query<UserCategorie>().First(c => c.ReductionName == "Service")
			};
			session.Save(engineer2);

			request.Performer = engineer2;
			var messages = request.GetEditSms(session);
			Assert.AreEqual(2, messages.Count);
			var cancelMessage = messages.First(m => m.Text.Contains("заявка отменена"));
			Assert.AreEqual("+7" + engineer1.TelNum, cancelMessage.PhoneNumber);
			var newMessage = messages.First(m => m.Text.Contains("9505001055"));
			Assert.AreEqual("+7" + engineer2.TelNum, newMessage.PhoneNumber);
		}
	}
}