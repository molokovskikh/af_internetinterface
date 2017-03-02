using System;
using System.IO;
using System.Linq;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using ContactType = InternetInterface.Models.ContactType;

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
			var client = ClientHelper.Client(session);
			session.Save(client);

			engineer1 = new Partner(Guid.NewGuid().ToString(), session.Load<UserRole>(3u)) {
				TelNum = "980-8791258",
				Role = session.Query<UserRole>().First(c => c.ReductionName == "Service")
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
			Assert.That(writeOff.Comment, Does.Contain("Оказание доп"));
		}

		[Test]
		public void Send_cancelation_on_performer_change()
		{
			var engineer2 = new Partner(Guid.NewGuid().ToString(), session.Load<UserRole>(3u)) {
				TelNum = "920-1564189",
				Role = session.Query<UserRole>().First(c => c.ReductionName == "Service")
			};
			session.Save(engineer2);

			request.Performer = engineer2;
			var messages = request.GetEditSms(session);
			Assert.AreEqual(2, messages.Count);
			var cancelMessage = messages.First(m => m.Text.Contains("заявка отменена"));
			Assert.AreEqual("+79808791258", cancelMessage.PhoneNumber);
			var newMessage = messages.First(m => m.Text.Contains("9505001055"));
			Assert.AreEqual("+79201564189", newMessage.PhoneNumber);
		}

		[Test]
		public void Send_sms_on_close()
		{
			request.Client.Contacts.Add(new Contact(request.Client, ContactType.SmsSending, "9794561231"));
			request.RegDate = new DateTime(2014, 08, 01);
			request.Status = ServiceRequestStatus.Close;
			request.Sum = 200;
			request.CloseSmsMessage = "переобжим коннектора со стороны абонента";
			var messages = request.GetEditSms(session);
			Assert.AreEqual(1, messages.Count);
			var message = messages[0];
			Assert.AreEqual("+79505001055", message.PhoneNumber);
			Assert.AreEqual(String.Format("С Вашего счета списано 200,00р. по сервисной заявке №{0} от 01.08.2014 переобжим коннектора со стороны абонента", request.Id), message.Text);
		}
	}
}