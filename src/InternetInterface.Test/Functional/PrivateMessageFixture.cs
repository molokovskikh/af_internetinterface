using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class PrivateMessageFixture : global::Test.Support.Web.WatinFixture2
	{
		[Test]
		public void Create_private_message()
		{
			var client = ClientHelper.Client();
			session.Save(client);
			Open("/PrivateMessages/ForClient?clientId={0}", client.Id);
			AssertText("Объявление для");
			Css("textarea[name=\"PrivateMessage.Text\"]").Value = "Тестовое сообщение";
			Click("Сохранить");
			AssertText("Сохранено");
		}
	}
}