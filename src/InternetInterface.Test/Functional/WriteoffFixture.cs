using System;
using System.Linq;
using Headless;
using NUnit.Framework;
using Rhino.Mocks;
using Test.Support.Selenium;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class WriteoffFixture : HeadlessFixture
	{
		[Test]
		public void View_write_off()
		{
			var page = Open();
			page = Click(page, "Списания");
			Assert.That(page.Html, Is.StringContaining("Имя клиента"));
		}
	}
}