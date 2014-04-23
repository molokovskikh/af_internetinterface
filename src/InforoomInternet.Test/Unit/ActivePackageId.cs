using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InforoomInternet.Models;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NUnit.Framework;

namespace InforoomInternet.Test.Unit
{
	[TestFixture]
	public class ActivePackageId
	{
		[Test]
		public void Update_active_package_id_test()
		{
			var endpoint = new ClientEndpoint();
			endpoint.UpdateActualPackageId(10);
			Assert.AreEqual(endpoint.ActualPackageId.Value, 10);
		}

		[Test]
		public void Sce_update_package_id_test()
		{
			var activeIdOne = SceHelper.Action("login", "testId", false, false, 10, "192.168.0.1");
			var activeIdTwo = SceHelper.Action("login", new ClientEndpoint { PackageId = 10, Client = new Client() }, "192.168.0.1");
			Assert.AreEqual(activeIdOne, 10);
			Assert.AreEqual(activeIdTwo, 10);
		}
	}
}
