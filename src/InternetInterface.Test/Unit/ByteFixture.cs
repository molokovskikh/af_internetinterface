using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Web.Ui.Helpers;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	class ByteFixture
	{
		[Test]
		public void ByteTest()
		{
			byte[] raw = BitConverter.GetBytes(Convert.ToInt64("2886730166"));
			var fg = new byte[8];
			fg[0] = 182;
			fg[1] = 1;
			fg[2] = 16;
			fg[3] = 172;
			string dfg = BitConverter.ToInt64(fg, 0).ToString();
			Assert.That(dfg, Is.StringContaining("2886730166"));
		}

		[Test]
		public void DateTest()
		{
			var day = DateTime.Now;
			var indexDay = (int) day.DayOfWeek;
			Console.WriteLine(day.AddDays(-indexDay+1));
			Console.WriteLine(day.AddDays(7 - indexDay));
		}
	}
}
