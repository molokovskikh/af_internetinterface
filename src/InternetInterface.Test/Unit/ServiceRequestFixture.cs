using System;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class ServiceRequestFixture
	{
		private ServiceRequest request;

		[SetUp]
		public void Setup()
		{
			request = new ServiceRequest();
			request.Contact = "950-5001055";
			request.Client = new Client();
			request.Performer = new Partner();

			InitializeContent.GetAdministrator = () => new Partner();
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
			Assert.That(request.Writeoff, Is.Not.Null);
			Assert.That(request.Writeoff.Sum, Is.EqualTo(200));
			Assert.That(request.Writeoff.Comment, Is.StringContaining("Оказание доп"));
		}
	}
}