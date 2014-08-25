using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Net;
using Castle.ActiveRecord.Framework;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NHibernate;
using NUnit.Framework;
using Castle.ActiveRecord;
using InforoomInternet.Helpers;
using System.Collections.Generic;
using Test.Support;

namespace InforoomInternet.Test.Integration
{
	[TestFixture]
	public class IPAdressHelperTest : IntegrationFixture
	{
		private ClientEndpoint testClientEndpoint;
		private List<object> deleteOnTeardown;

		[SetUp]
		public void Setup()
		{
			testClientEndpoint = new ClientEndpoint();
			session.Save(testClientEndpoint);
			deleteOnTeardown = new List<object> {
				testClientEndpoint
			};
		}
		
		[Test(Description = "Тестирование при арендованом IP")]
		public void Test_Lease()
		{
			var testLease = new Lease() {
				Endpoint = testClientEndpoint,
				Ip = IPAddress.Parse("87.137.102.106")
			};
			session.Save(testLease);
			deleteOnTeardown.Add(testLease);

			Assert.IsNotNull(IpAdressHelper.GetClientEndpoint("87.137.102.106", session));
		}

		[Test(Description = "Тестирование при статическом IP")]
		public void Test_Static()
		{
			var testStaticIp = new StaticIp() {
				EndPoint = testClientEndpoint,
				Ip = "87.137.152.106"
			};
			session.Save(testStaticIp);
			deleteOnTeardown.Add(testStaticIp);

			Assert.IsNotNull(IpAdressHelper.GetClientEndpoint("87.137.152.106", session));
		}

		[Test(Description = "Тестирование при нахождении IP в подсети")]
		public void Test_Subnet()
		{
			var testStaticIpSubNet = new StaticIp() {
				EndPoint = testClientEndpoint,
				Ip = "87.137.132.0",
				Mask = 24
			};
			session.Save(testStaticIpSubNet);
			deleteOnTeardown.Add(testStaticIpSubNet);

			Assert.IsNotNull(IpAdressHelper.GetClientEndpoint("87.137.132.106", session));
		}

		[TearDown]
		public void Teardown()
		{
			session.DeleteMany(deleteOnTeardown.ToArray());
		}
	}
}