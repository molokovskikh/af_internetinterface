using System;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class ServiceRequestFixture
	{
		[Test]
		public void Sms_should_contains_perform_date_time()
		{
			var request = new ServiceRequest();
			request.Contact = "950-5001055";
			request.Client = new Client();
			request.Performer = new Partner();
			request.PerformanceDate = new DateTime(2012, 05, 21, 17, 00, 00);
			var sms = request.GetSms();
			Assert.That(sms.Text, Is.StringStarting("$ 21.05.2012 17:00:00"));
		}
	}
}