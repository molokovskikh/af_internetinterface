﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Castle.ActiveRecord;
using Castle.MonoRail.TestSupport;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers;
using InternetInterface.Models;
using InternetInterface.Test.Functional;
using InternetInterface.Test.Helpers;
using NHibernate;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	internal class HouseMapFixture : ControllerFixture
	{
		[Test]
		public void ViewTest()
		{
			var mapController = new HouseMapController();
			Prepare(mapController);
			mapController.ViewHouseInfo();
		}
	}
}