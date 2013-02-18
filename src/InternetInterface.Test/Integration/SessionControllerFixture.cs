using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.MonoRail.Framework;
using Castle.MonoRail.Framework.Routing;
using Castle.MonoRail.Framework.Services;
using Castle.MonoRail.Framework.Test;
using Castle.MonoRail.TestSupport;
using NHibernate;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class SessionControllerFixture : BaseControllerTest
	{
		protected ISession session;
		protected SessionScope scope;
		protected ISessionFactoryHolder sessionHolder;
		private string referer;

		[SetUp]
		public void SetUp()
		{
			referer = "http://www.ivrn.net/";
			scope = new SessionScope();
			sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
		}

		protected override IMockResponse BuildResponse(UrlInfo info)
		{
			return new StubResponse(
				info,
				new DefaultUrlBuilder(),
				new StubServerUtility(),
				new RouteMatch(),
				referer);
		}

		[TearDown]
		public void TearDown()
		{
			sessionHolder.ReleaseSession(session);
			scope.Dispose();
		}
	}
}
