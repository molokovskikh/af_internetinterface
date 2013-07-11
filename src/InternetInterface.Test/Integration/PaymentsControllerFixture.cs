using System;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration
{
	public class PaymentsControllerFixture : ControllerFixture
	{
		private PaymentsController controller;
		private DataMother data;

		[SetUp]
		public void Setup()
		{
			controller = new PaymentsController();
			data = new DataMother(session);
			Prepare(controller);
		}

		[Test]
		public void Move_payment()
		{
			var destination = data.PhysicalClient();
			session.Save(destination);
			var source = data.PhysicalClient();
			session.Save(source);
			var payment = new Payment(source, 100);
			session.Save(payment);

			Request.Params["action.Comment"] = "test";
			Request.Params["action.Destination.Id"] = destination.Id.ToString();
			controller.Move(payment.Id);
			session.Flush();

			session.Refresh(destination);
			Assert.AreEqual(1, destination.Payments.Count);
			Assert.AreEqual(100, destination.Payments[0].Sum);
			Assert.That(email.Body, Is.StringContaining(String.Format("От клиент: {0}", source)));
			Assert.That(email.Body, Is.StringContaining(String.Format("К клиенту: {0}", destination)));
		}
	}
}