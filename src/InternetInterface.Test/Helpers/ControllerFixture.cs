using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Castle.MonoRail.TestSupport;
using Common.Tools;
using Common.Web.Ui.Controllers;
using NHibernate;
using NUnit.Framework;

namespace InternetInterface.Test.Helpers
{
	public class ControllerFixture : BaseControllerTest
	{
		//protected FakeContext fakeContext;
		protected SessionScope scope;
		protected ISession session;

		[SetUp]
		public void SetupFixture()
		{
			scope = new SessionScope();
			session = ActiveRecordMediator.GetSessionFactoryHolder().CreateSession(typeof(ActiveRecordBase));
			//fakeContext = new FakeContext();
			//AppContext.Context = fakeContext;
		}

		[TearDown]
		public void TearDown()
		{
			if (session != null) {
				ActiveRecordMediator.GetSessionFactoryHolder().ReleaseSession(session);
				session = null;
			}
			scope.SafeDispose();
			scope = null;
		}

		protected void Prepare(BaseController controller)
		{
			controller.DbSession = session;
			PrepareController(controller);
		}
	}
}
