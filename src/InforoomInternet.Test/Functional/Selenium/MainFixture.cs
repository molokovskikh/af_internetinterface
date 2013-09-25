using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Test.Support.Selenium;
using NUnit.Framework;
using Test.Support.Web;

namespace InforoomInternet.Test.Functional.Selenium
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
