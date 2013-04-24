using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Net;
using InternetInterface.Helpers;
using NUnit.Framework;

namespace InforoomInternet.Test.Unit
{
	[TestFixture]
	public class IPTypeConverterFixture
	{
		[Test]
		public void Convert()
		{
			TypeDescriptor.AddProvider(new IPAddressTypeDescriptorProvider(), typeof(IPAddress));
			var conv = TypeDescriptor.GetConverter(typeof(IPAddress));
			Assert.IsTrue(conv.CanConvertFrom(typeof(string)));

			Assert.AreEqual(IPAddress.Loopback, conv.ConvertFrom(IPAddress.Loopback.ToString()));
		}
	}
}