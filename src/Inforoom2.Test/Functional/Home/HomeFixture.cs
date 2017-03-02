using System;
using Inforoom2.Models;
using Inforoom2.Test.Infrastructure;
using NUnit.Framework;
using OpenQA.Selenium;

namespace Inforoom2.Test.Functional.Home
{
	/// <summary>
	/// Проверка на главной странице
	/// </summary>
	[TestFixture]
	public class HomeIndexFixture : BaseFixture
	{
		protected Question Question;

		[Test, Description("Проверка переадресации на 'Заявка на подключение'")]
		public void CheckRedirectToClientRequest()
		{
			Open();
			var link = browser.FindElement(By.CssSelector("div[class='header'] input.connect"));
			link.Click();

			AssertText("ЗАЯВКА НА ПОДКЛЮЧЕНИЕ");
		}
	}
}