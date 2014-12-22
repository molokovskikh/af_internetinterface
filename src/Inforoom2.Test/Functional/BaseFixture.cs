using System;
using System.Reflection;
using System.Web;
using Inforoom2.Components;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Cfg;
using NUnit.Framework;
using OpenQA.Selenium;
using Test.Support.Selenium;

namespace Inforoom2.Test.Functional
{
	[TestFixture]
	public class BaseFixture : SeleniumFixture
	{
		protected new ISession session
		{
			get { return MvcApplication.SessionFactory.OpenSession(); }
		}

		[SetUp]
		public override void IntegrationSetup()
		{
			//Ставим куки, чтобы не отображался popup
		SetCookie("userCity", BuildTestUrl("Белгород"));
		}

		[TearDown]
		public override void IntegrationTearDown()
		{
			Close();
		}

		protected override void Close()
		{
			session.Close();
		}

		protected bool IsTextExists(string text)
		{
			var body = browser.FindElementByCssSelector("body").Text;
			return body.Contains(text);
		}

		public void SetCookie(string name, string value)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(value);
			var text = System.Convert.ToBase64String(plainTextBytes);
			var cookie = new Cookie(name, text);
			browser.Manage().Cookies.AddCookie(cookie);
		}
	
		protected string GetCookie(string cookieName)
		{
			var cookie = browser.Manage().Cookies.GetCookieNamed(cookieName);
			if (cookie == null) {
				return string.Empty;
			}

			var base64EncodedBytes = System.Convert.FromBase64String(cookie.Value);
			return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
			
		}
	}
}