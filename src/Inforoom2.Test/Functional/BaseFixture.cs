using System;
using System.Reflection;
using System.Web;
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
			get { return NHibernateActionFilter.sessionFactory.OpenSession(); }
		}

		[SetUp]
		public override void IntegrationSetup()
		{
			//Ставим куки, чтобы не отображался popup
			var cookie = new Cookie("userCity", "Воронеж");
			browser.Manage().Cookies.AddCookie(cookie);
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
	}
}