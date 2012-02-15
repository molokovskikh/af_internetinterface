﻿using System;
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
				var filter = new AccessFilter();
				Request.UserHostAddress = "192.168.200.1";
				Assert.IsTrue(filter.Perform(ExecuteWhen.BeforeAction, controller.Context, controller, controller.ControllerContext));
			}
		}
	}
}
