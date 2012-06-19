using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture, Ignore("Чинить")]
	public class SmsFixture : global::Test.Support.Web.WatinFixture2
	{
		[Test]
		public void ShowTest()
		{
			var client = Client.FindFirst();
			Open("Sms/SmsIndex?clientId=" + client.Id);
			Assert.That(browser.Text, Is.StringContaining("Sms сообщения пользователя"));
			browser.TextField("sms_text").AppendText("test sms");
			browser.Button("send_but").Click();
			Assert.That(browser.Text, Is.StringContaining("Сообщение передано для отправки"));
		}
	}
}
