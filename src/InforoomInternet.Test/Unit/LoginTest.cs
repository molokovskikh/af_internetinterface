using System;
using System.Net;
using Common.Tools;
using InternetInterface.Models;
using NUnit.Framework;

namespace InforoomInternet.Test.Unit
{
	[TestFixture]
	public class LoginTest
	{
		[Test]
		public void IpRangeTest()
		{
			var ip = new IPAddress(RangeFinder.reverseBytesArray(Convert.ToUInt32(NetworkSwitches.SetProgramIp("91.219.6.220"))));
			var host = SubnetMask.CreateByNetBitLength(29);

			var netw = ip.GetNetworkAddress(host);

			Assert.IsTrue(ip.IsInSameSubnet(netw, host));
		}
	}
}