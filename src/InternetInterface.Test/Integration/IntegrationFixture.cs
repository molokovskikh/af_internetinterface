using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class IntegrationFixture
	{
		protected ISessionScope scope;

		[SetUp]
		public void Setup()
		{
			scope = new SessionScope();
		}

		[TearDown]
		public void TearDown()
		{
			if (scope != null)
				scope.Dispose();
		}
	}
}
