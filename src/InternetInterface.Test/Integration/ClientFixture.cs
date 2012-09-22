using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class ClientFixture : IntegrationFixture
	{
		[Test]
		public void Ger_write_offs_test()
		{
			var client = ClientHelper.Client();
			session.SaveOrUpdate(client);
			var writeOff = new WriteOff(client, 500m);
			var userWriteOff = new UserWriteOff(client, 100m, "testUserWriteOff");
			session.Save(writeOff);
			session.Save(userWriteOff);
			Flush();
			var writeOffs = client.GetWriteOffs(session, string.Empty);
			Assert.AreEqual(writeOffs.Count, 2);
			Assert.That(writeOffs[0].WriteOffSum, Is.EqualTo(500m));
			Assert.That(writeOffs[1].WriteOffSum, Is.EqualTo(100m));
			Assert.That(writeOffs[1].Comment, Is.EqualTo("testUserWriteOff"));
		}
	}
}
