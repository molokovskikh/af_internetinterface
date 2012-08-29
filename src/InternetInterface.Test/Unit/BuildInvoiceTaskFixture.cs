using System;
using Common.Tools;
using Common.Tools.Calendar;
using InternetInterface.Background;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class BuildInvoiceTaskFixture
	{
		[Test]
		public void Start_in_next_month()
		{
			var task = new BuildInvoiceTask(null);
			Assert.That(task.Ready(), Is.False);
			SystemTime.Now = () => DateTime.Now.AddMonths(1).FirstDayOfMonth();
			Assert.That(task.Ready(), Is.True);
		}
	}
}