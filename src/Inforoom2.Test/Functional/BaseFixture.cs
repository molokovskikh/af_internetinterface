using System;
using System.Reflection;
using System.Web;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Cfg;
using NUnit.Framework;
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
		public override void  IntegrationSetup()
		{
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
	}
}