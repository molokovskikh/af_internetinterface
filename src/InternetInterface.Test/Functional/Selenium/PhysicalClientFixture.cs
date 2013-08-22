using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;

namespace InternetInterface.Test.Functional.Selenium
{
	[TestFixture]
	class PhysicalClientFixture : AcceptanceFixture
	{
		[Test]
		public void NoOrderForPhysicalClient()
		{
			Css("input[name='EditConnectFlag'] + button").Click();
			Css("input#Port").SendKeys("1");
			Css("input#Submit2").Click();
			var order = session.Get<Order>(client.Id);
			Assert.Null(order);
		}

		[Test]
		public void Calendar_in_begin_date()
		{
			ClickLink("Статистика работы");
			Assert.NotNull(Css("input.hasDatepicker#beginDate"));
		}
	}
}
