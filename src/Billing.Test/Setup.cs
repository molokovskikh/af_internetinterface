using Billing.Test.Integration;
using Common.Tools;
using NUnit.Framework;

namespace Billing.Test
{
	[SetUpFixture]
	public class Setup
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			MainBilling.InitActiveRecord();
			MainBillingFixture.SeedDb();
		}

		[OneTimeTearDown]
		public void Teardown()
		{
			SystemTime.Reset();
			//очищаем за собой что бы тесты в internetinterface могли создать данные для себя
			MainBillingFixture.CleanDbAfterTest();
		}
	}
}