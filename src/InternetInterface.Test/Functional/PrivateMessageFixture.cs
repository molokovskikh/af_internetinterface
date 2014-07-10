using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class PrivateMessageFixture : SeleniumFixture
	{
		[Test]
		public void Create_private_message()
		{
			var client = ClientHelper.Client(session);
			session.Save(client);
			Open("/PrivateMessages/ForClient?clientId={0}", client.Id);
			AssertText("Объявление для");
			Css("textarea[name=\"PrivateMessage.Text\"]").SendKeys("Тестовое сообщение");
			Click("Сохранить");
			AssertText("Сохранено");
		}
	}
}
