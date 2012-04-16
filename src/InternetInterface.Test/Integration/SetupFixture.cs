using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Integration
{
	[SetUpFixture]
	class SetupFixture
	{
		[SetUp]
		public void Setup()
		{
			WatinFixture.ConfigTest();
		}
	}
}
