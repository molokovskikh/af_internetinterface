using System;
using System.Linq;
using System.Web.UI.WebControls;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium;

namespace Inforoom2.Test.Functional
{
	[TestFixture]
	public class HomeFixture : BaseFixture
	{
		protected Question Question;

		[Test, Description("Проверка определения города")]
		public void CitySelectTest()
		{
			Open();
			string js = @"cli.setCookie('userCity')";
			browser.ExecuteScript(js);
			Open();
			AssertText("ВЫБЕРИТЕ ГОРОД");
			var bt = browser.FindElement(By.XPath("//div[@class='buttons']//button[@class='button cancel']"));
			bt.Click();
			var link = browser.FindElement(By.XPath("//div[@class='cities']//a[text()='Борисоглебск']"));
			link.Click();
			var cookie = browser.Manage().Cookies.GetCookieNamed("userCity");
			Assert.That(cookie.Value, Is.EqualTo("Борисоглебск"));
		}
	}
}