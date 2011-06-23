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

	    [Test]
		public void ImageTest()
		{

			//IsAuthenticated("test123", "0o9i8u7y6t");
			//IsAuthenticated("Diller1", "1q2w3e4r5t");
			//ChangePassword("test123", "0o9i8u7y6t");
			StreamReader sr = new StreamReader("c:\\test.txt", Encoding.UTF8);
			while (sr.Peek() != -1)
			{
				var Line = sr.ReadLine();      // Line = sr.ReadToEnd();
				Console.WriteLine(Line.Replace("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", "***"));
			}
			sr.Dispose();    // sr.Close();
		}
	}
}
