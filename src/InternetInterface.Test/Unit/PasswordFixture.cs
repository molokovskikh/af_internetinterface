using System;
using System.Collections.Generic;
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
				passwords.Add(CryptoPass.GeneratePassword());
			}
			Assert.AreEqual(passwords.GroupBy(g => g).Count(), 5);
		}
	}
}
