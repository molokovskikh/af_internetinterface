using System;
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
		public HouseMapFixture()
		{
			Setup.ConfigTest();
		}

		[Test]
		public void ViewTest()
		{
			using (new SessionScope()) {
				var mapController = new HouseMapController();
				PrepareController(mapController);
				mapController.DbSession = session;
				mapController.ViewHouseInfo();
			}
		}

		[Test]
		public void AtributeTest()
		{
			string test = "abcdefghijklmnopqrstuvwxyz";

			// To retrieve the value of the indexed Chars property using
			// reflection, first get a PropertyInfo for Chars.
			PropertyInfo pinfo = typeof(string).GetProperty("Chars");

			// To retrieve an instance property, the GetValue method
			// requires the object whose property is being accessed and an
			// array of objects representing the index values.

			// Show the seventh letter (g)
			object[] indexArgs = { 6 };
			object value = pinfo.GetValue(test, indexArgs);
		}

		[Test]
		public void InitializeHelperTest()
		{
			using (new SessionScope()) {
				var pclient = PhysicalClient.FindFirst();
				Assert.IsTrue(NHibernateUtil.IsInitialized(pclient.Tariff));
			}
		}
	}
}