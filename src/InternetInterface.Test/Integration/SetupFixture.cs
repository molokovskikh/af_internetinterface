using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test
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
