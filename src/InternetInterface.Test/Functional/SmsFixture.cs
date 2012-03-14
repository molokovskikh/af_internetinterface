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
	public class SmsFixture : WatinFixture2
	{
		[Test]
		public void ShowTest()
		{
			var client = Client.FindFirst();
			using (var browser = Open("Sms/SmsIndex?clientId=" + client.Id)) {
				Assert.That(browser.Text, Is.StringContaining("Sms сообщения пользователя"));
				browser.TextField("sms_text").AppendText("test sms");
				browser.Button("send_but").Click();
				Assert.That(browser.Text, Is.StringContaining("Сообщение передано для отправки"));
			}
		}
	}
}
