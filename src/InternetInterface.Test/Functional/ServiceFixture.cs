using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class ServiceFixture : WatinFixture2
	{
		[Test]
		public void BaseFunctional()
		{
			var client = Client.FindFirst();
			using (var browser = Open(string.Format("UserInfo/SearchUserInfo.rails?filter.ClientCode={0}", client.Id))) {
				browser.Link("createServiceLink").Click();
				browser.TextField("DescriptionText").AppendText("test");
				browser.TextField("contact_text").AppendText("900-9090900");
				browser.Button("register_button").Click();
				Assert.That(browser.Text, Is.StringContaining("Информация по клиенту"));
			}
		}

		[Test]
		public void ViewRequests()
		{
			using (var browser = Open("ServiceRequest/ViewRequests")) {
				Assert.That(browser.Text, Is.StringContaining("Фильтр"));
				new ServiceRequest {
					Contact = "900-9090900"
				}.Save();
				browser.Refresh();
				Assert.That(browser.Text, Is.StringContaining("900-9090900"));
			}
		}
	}
}
