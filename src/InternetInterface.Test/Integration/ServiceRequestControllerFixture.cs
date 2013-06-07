using System;
using System.Collections.Generic;
using System.Linq;
using Castle.MonoRail.TestSupport;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class ServiceRequestControllerFixture : ControllerFixture
	{
		private ServiceRequestController controller;

		[SetUp]
		public void Setup()
		{
			controller = new ServiceRequestController();
			Prepare(controller);
		}

		[Test]
		public void Send_sms_on_request_cancelation()
		{
			var client = ClientHelper.Client();
			session.Save(client);

			var performer = Partner.GetServiceIngeners().First();
			if (String.IsNullOrEmpty(performer.TelNum)) {
				performer.TelNum = "473-2606000";
				session.Save(performer);
			}
			var request = new ServiceRequest {
				Performer = performer,
				Registrator = InitializeContent.Partner,
				Client = client,
				Contact = "473-2606000"
			};
			session.Save(request);
			request.Status = ServiceRequestStatus.Cancel;
			controller.EditServiceRequest(request);
			Assert.AreEqual(1, sms.Count);
			Assert.That(sms[0], Is.StringContaining(String.Format("сч. {0} заявка отменена", request.Client.Id)));
		}
	}
}