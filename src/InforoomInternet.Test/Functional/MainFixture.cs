using NUnit.Framework;
using Test.Support.Web;
using WatiN.Core.Native.Windows;

namespace InforoomInternet.Test.Functional
{
	[TestFixture, Ignore("Тесты перенесены в Selenium")]
	public class MainFixture : WatinFixture2
	{
		[Test]
		public void FeedBackTest()
		{
			Open("/Main/Feedback");
			Css("#appealText").AppendText("TestAppeal");
			Css("#clientName").AppendText("TestFio");
			Css("#contactInfo").AppendText("TestAppeal@app.net");
			Css("#saveFeedback").Click();
			AssertText("Спасибо, Ваша заявка принята.");
		}
	}
}