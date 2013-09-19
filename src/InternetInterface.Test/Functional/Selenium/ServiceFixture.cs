﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional.Selenium
{
	class ServiceFixture : SeleniumFixture
	{
		private Client client;

		[SetUp]
		public void Setup()
		{
			client = ClientHelper.Client();
			session.Save(client);
		}

		[Test]
		public void BaseFunctional()
		{
			Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", client.Id));
			Click("Сервисная заявка");

			Css("textarea[name=\"request.Description\"]").SendKeys("test");
			Css("input[name=\"request.Contact\"]").SendKeys("900-9090900");
			Css("input[name=\"request.PerformanceDate\"]").SendKeys("21.05.2012");
			Css("input[name=\"request.PerformanceTime\"]").SendKeys("10:00");
			Click("Сохранить");
			AssertText("Информация по клиенту");

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
			AssertText("Фильтр");
			AssertText("900-9090900");
		}

		[Test]
		public void Filter_test()
		{
			var request = new ServiceRequest {
				Contact = "900-9090900",
				Client = client
			};
			session.Save(request);
			Open("ServiceRequest/ViewRequests");

			browser.FindElementById("filter_Text").SendKeys("test_text");
			Click("Применить");
			AssertText("По вашему запросу ничего не найдено, либо вы не ввели информацию для поиска");
			browser.FindElementById("filter_Text").Clear();
			browser.FindElementById("filter_Text").SendKeys(client.Id.ToString());
			Click("Применить");
			AssertText("Фильтр");
			AssertText("900-9090900");
		}
	}
}
