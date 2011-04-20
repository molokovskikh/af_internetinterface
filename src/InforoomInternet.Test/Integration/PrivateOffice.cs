using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Castle.MonoRail.TestSupport;
using InforoomInternet.Controllers;
using InforoomInternet.Models;
using InternetInterface;
using InternetInterface.Models;
using log4net.Config;
using NUnit.Framework;
using log4net;
namespace InforoomInternet.Test.Integration
{
	[TestFixture]
	class PrivateOfficeTest : BaseControllerTest 
	{
		private PrivateOffice controller;

		[SetUp]
		public void Init()
		{
			controller = new PrivateOffice();
			PrepareController(controller);
		}


		[Test]
		public void EditMenu()
		{
			using (new SessionScope())
			{
				var editController = new EditorController();
				PrepareController(editController);
				//Request.
				/*editController.Menu();
				Console.WriteLine(editController.PropertyBag["Content"]);
				Assert.That(countVhog(editController.PropertyBag["Content"].ToString(), "BRBABRBABRBA"), Is.EqualTo(4));
				Assert.That(countVhog(editController.PropertyBag["Content"].ToString(), "ARARARARARAR"), Is.EqualTo(4));*/
			}
		}

		private int countVhog(string text, string virog)
		{
			var count = 0;
			var lastVh = 0;
			while (lastVh >= 0)
			{
				lastVh = text.IndexOf(virog, lastVh + 1);
				count++;
			}
			return count - 1;
		}



		[Test]
		public void ProvateOffice()
		{
			using (new SessionScope())
			{
				/*var phisClient = new PhysicalClients
				                 	{
				                 		Name = "Test",
										Password = CryptoPass.GetHashString("123")
				                 	};*/
				var phisClient = PhysicalClients.Find((uint) 13);
				phisClient.Password = CryptoPass.GetHashString("123");
				phisClient.UpdateAndFlush();
				/*var client = new Clients
				             	{
				             		PhisicalClient = phisClient,
									Name = "test",
									Disabled = false
				             	};
				var endPoint = new ClientEndpoints
				               	{
				               		Client = client,
				               		Disabled = false
				               	};
				var newLease = new Lease
				               	{
									Endpoint = endPoint,
				               		Ip = Convert.ToUInt32(NetworkSwitches.SetProgramIp("91.219.7.3"))
				               	};
				phisClient.SaveAndFlush();*/
				/*client.SaveAndFlush();
				endPoint.SaveAndFlush();
				newLease.SaveAndFlush();*/
				/*var filter = new AccessFilter();
				Request.UserHostAddress = "91.219.7.3";
				Assert.IsTrue(filter.Perform(ExecuteWhen.BeforeAction, controller.Context, controller, controller.ControllerContext));
				client.DeleteAndFlush();
				endPoint.DeleteAndFlush();
				newLease.DeleteAndFlush();*/
			}
		}
	}
}
