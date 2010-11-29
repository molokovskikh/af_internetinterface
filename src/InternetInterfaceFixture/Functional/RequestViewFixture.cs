using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterfaceFixture.Helpers;
using NUnit.Framework;

namespace InternetInterfaceFixture.Functional
{
	[TestFixture]
	class RequestViewFixture : WatinFixture
	{
		[Test]
		public void ViewTest()
		{
			using (var browser = Open("UserInfo/RequestView.rails"))
			{
				Assert.That(browser.Text, Is.StringContaining("Самостоятельность"));
				Assert.That(browser.Text, Is.StringContaining("Email"));
				Assert.That(browser.Text, Is.StringContaining("Адрес"));
			}
		}
	}
}
