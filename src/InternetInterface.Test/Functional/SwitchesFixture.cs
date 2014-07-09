using System;
using System.Linq;
using System.Threading;
using Common.Tools.Calendar;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace InternetInterface.Test.Functional
{
	[TestFixture]
	public class SwitchesFixture : ClientFunctionalFixture
	{
		[Test]
		public void Filter_switches_on_online_page()
		{
			var zone = session.Query<Zone>().First();
			var overZone = new NetworkSwitch(Guid.NewGuid().ToString(), session.Query<Zone>().First(z => z != zone));
			session.Save(overZone);

			Open("Switches/OnLineClient");
			AssertText("Параметры фильтрации");

			var options = browser.FindElementsByCssSelector("#filter_Switch_Id option").Select(e => e.Text).ToArray();
			Assert.Contains(overZone.Name, options);

			Css("#filter_Zone_Id").SelectByValue(zone.Id.ToString());
			WaitAjax();
			options = browser.FindElementsByCssSelector("#filter_Switch_Id option").Select(e => e.Text).ToArray();
			Assert.That(options, Is.Not.Contains(overZone.Name));
		}
	}
}