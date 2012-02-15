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
		[Test]
		public void GenPass()
		{
			Console.WriteLine(CryptoPass.GetHashString("1234"));
		}

		/*[Test]
		public void ImageTest()
		{
			StreamReader sr = new StreamReader("c:\\test.txt", Encoding.UTF8);
			while (sr.Peek() != -1)
			{
				var Line = sr.ReadLine();
				Console.WriteLine(Line.Replace("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", "***"));
			}
			sr.Dispose();
		}*/
	}
}
