using NUnit.Framework;
using Test.Support.Selenium;

namespace InforoomInternet.Test.Functional
{
	[TestFixture]
	class MainFixture : SeleniumFixture
	{
		[Test]
		public void FeedBackTest()
		{
			Open("/Main/Feedback");
			Css("#appealText").SendKeys("TestAppeal");
			Css("#clientName").SendKeys("TestFio");
			Css("#contactInfo").SendKeys("TestAppeal@app.net");
			Css("#saveFeedback").Click();
			AssertText("Спасибо, Ваша заявка принята.");
		}
	}
}
