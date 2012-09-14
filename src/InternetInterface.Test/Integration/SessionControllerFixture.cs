using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
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

		[SetUp]
		public void SetUp()
		{
			scope = new SessionScope();
			sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
		}

		[TearDown]
		public void TearDown()
		{
			sessionHolder.ReleaseSession(session);
			scope.Dispose();
		}
	}
}
