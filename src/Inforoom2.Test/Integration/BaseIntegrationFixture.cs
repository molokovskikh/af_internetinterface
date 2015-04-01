using System;
using System.Collections.Specialized;
using System.Reflection;
using System.Web;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.Routing;
using NHibernate;
using NUnit.Framework;
using Test.Support;

namespace Inforoom2.Test.Integration
{
	public abstract class BaseIntegrationFixture : IntegrationFixture
	{
		protected new ISession session;

		[SetUp]
		public override void IntegrationSetup()
		{
			MvcApplication.InitializeSessionFactory();
			MvcApplication.RegisterRoutes(RouteTable.Routes);
			typeof(BuildManager).GetProperty("PreStartInitStage", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, 2, null);
			typeof(BuildManager).GetField("_topLevelFilesCompiledStarted", BindingFlags.NonPublic | BindingFlags.Instance).
				SetValue(typeof(BuildManager).GetField("_theBuildManager", BindingFlags.NonPublic | BindingFlags.Static).
					GetValue(null), true);

			AreaRegistration.RegisterAllAreas();
			session = MvcApplication.SessionFactory.OpenSession();
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