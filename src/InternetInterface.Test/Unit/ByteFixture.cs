using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace InternetInterfaceFixture.Unit
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
			var qw = Convert.ToByte(Convert.ToInt64("2886730166"));
		}
	}
}
