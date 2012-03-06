using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NUnit.Framework;
using InternetInterface.Controllers;

namespace InternetInterface.Test.Unit_
{
	[TestFixture]
	class LoginFixture : ActiveDirectoryHelper
	{
		public class MyClass
		{
			public int i;
		}

		public void t1(MyClass c, MyClass c1)
		{
			c = c1;
			c1.i = 5;
		}

		[Test]
		public void MyTest()
		{
			MyClass c = null;
			MyClass c1 = new MyClass();
			t1(c, c1);
			Console.WriteLine(c1.i);
		}

		[Test]
		public void GenPass()
		{
			Console.WriteLine(CryptoPass.GetHashString("1234"));
		}

		[Test]
		public void Date_Time()
		{
			var d1 = DateTime.Now;
			var d2 = DateTime.Now.AddMonths(-1);
			var spin = (d1 - d2).Ticks;
			Console.WriteLine(new DateTime(spin));
		}

		[Test]
		public void ImageTest()
		{
			var sr = new StreamReader("c:\\test.txt", Encoding.UTF8);
			while (sr.Peek() != -1) {
				var line = sr.ReadLine();
				if (line != null)
					Console.WriteLine(line.Replace("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", "***"));
			}
			sr.Dispose();
		}
	}
}
