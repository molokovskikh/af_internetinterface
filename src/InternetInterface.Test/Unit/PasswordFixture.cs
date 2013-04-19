using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	[TestFixture]
	public class PasswordFixture
	{
		[Test]
		public void Generate_Any_password()
		{
			var passwords = new List<string>();
			for (int i = 0; i < 5; i++) {
				passwords.Add(CryptoPass.GeneratePassword((uint)i * 1000));
			}
			Assert.AreEqual(passwords.GroupBy(g => g).Count(), 5);
		}

		[Test]
		public void Test()
		{
			Console.WriteLine(File.ReadAllText("c:/1.txt"));
		}
	}
}
