using Billing.Test.Integration;
using Common.Tools;
using NUnit.Framework;

namespace Billing.Test
{
	[SetUpFixture]
	public class Setup
	{
		[SetUp]
		public void SetupFixture()
		{
			MainBilling.InitActiveRecord();
			MainBillingFixture.PrepareTests();
		}

		[TearDown]
		public void Teardown()
		{
			SystemTime.Reset();
		}
	}
}
