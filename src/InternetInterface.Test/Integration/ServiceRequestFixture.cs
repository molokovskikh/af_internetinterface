using System;
using System.IO;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class ServiceRequestFixture : IntegrationFixture
	{
		private ServiceRequest request;

		[SetUp]
		public void Setup()
		{
			var client = ClientHelper.Client();
			session.Save(client);
			request = new ServiceRequest {
				Contact = "950-5001055",
				Client = client,
				Performer = InitializeContent.Partner
			};
			session.Save(request);
		}

		[Test]
		public void Sms_should_contains_perform_date_time()
		{
			request.PerformanceDate = new DateTime(2012, 05, 21, 17, 00, 00);
			var sms = request.GetSms();
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
	}
}