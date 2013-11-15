using NUnit.Framework;
using Test.Support.Selenium;

namespace InforoomInternet.Test.Functional
{
	[TestFixture]
	class Error404Fixture : SeleniumFixture
	{
		[SetUp]
		public void Setup()
		{
			defaultUrl = "/nosuchpage?error_fixture";
		}

		[Test]
		public void Check404()
		{
			AssertText("Страница не найдена");
		}
	}
}
