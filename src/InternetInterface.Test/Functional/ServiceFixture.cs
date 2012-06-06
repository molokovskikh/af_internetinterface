using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class ServiceFixture : global::Test.Support.Web.WatinFixture2
	{
		private Client client;

		[SetUp]
		public void Setup()
		{
			client = ClientHelper.Client();
			Save(client);
		}

		[Test]
		public void BaseFunctional()
		{
			Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", client.Id));
			Click("Сервисная заявка");

			Css("textarea[name=\"request.Description\"]").Value = "test";
			Css("input[name=\"request.Contact\"]").Value = "900-9090900";
			Css("input[name=\"request.PerformanceDate\"]").Value = "21.05.2012";
			Css("input[name=\"request.PerformanceTime\"]").Value = "10:00";
			Click("Сохранить");

			Assert.That(browser.Text, Is.StringContaining("Информация по клиенту"));

			var request = session.Query<ServiceRequest>().Where(r => r.Client == client).ToArray().Last();
			Assert.That(request.PerformanceDate.ToString(), Is.EqualTo("21.05.2012 10:00:00"));
			Assert.That(request.PerformanceTime.ToString(), Is.EqualTo("10:00:00"));
		}

		[Test]
		public void ViewRequests()
		{
			var request = new ServiceRequest {
				Contact = "900-9090900",
				Client = client
			};
			session.Save(request);

			Open("ServiceRequest/ViewRequests");
			Assert.That(browser.Text, Is.StringContaining("Фильтр"));
			Assert.That(browser.Text, Is.StringContaining("900-9090900"));
		}
	}
}
