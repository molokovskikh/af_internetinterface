using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.MonoRail.Framework;
using Castle.MonoRail.TestSupport;
using InforoomInternet.Controllers;
using NUnit.Framework;

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
		public void ProvateOffice()
		{
			using (new SessionScope())
			{
				var filter = new AccessFilter();
				Request.UserHostAddress = "91.219.7.3";
				Assert.IsTrue(filter.Perform(ExecuteWhen.BeforeAction, controller.Context, controller, controller.ControllerContext));
			}
		}
	}
}
