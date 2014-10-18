using System;
using System.Linq;
using System.Web.UI.WebControls;
using Inforoom2.Helpers;
using Inforoom2.Models;
using NHibernate.Linq;
using NUnit.Framework;

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
			AssertText("Ваш город");
			var link = browser.FindElementByCssSelector("#cityLink");
			Assert.That(link.Text, Is.EqualTo("Воронеж"));
			link.Click();
			WaitForVisibleCss(".selectCity");
			var cityLink = browser.FindElementByCssSelector(".selectCity");
			cityLink.Click();
			var cities = browser.FindElementsByCssSelector(".city");
			foreach(var city in cities)
				if(city.Text == "Борисоглебск")
					city.Click();
			Open("Faq");
			AssertText("Ваш город");
			link = browser.FindElementByCssSelector("#cityLink");
			Assert.That(link.Text, Is.EqualTo("Борисоглебск"));
		}

	}
}