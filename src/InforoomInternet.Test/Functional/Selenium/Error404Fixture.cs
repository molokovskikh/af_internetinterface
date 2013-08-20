using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support.Selenium;

namespace InforoomInternet.Test.Functional.Selenium
{
	[TestFixture]
	class Error404Fixture : SeleniumFixture
	{
		[SetUp]
		public void Setup()
		{
			defaultUrl = "/nosuchpage";
		}

		[Test]
		public void Check404()
		{
			AssertText("Адрес введен неправильно");
		}
	}
}
