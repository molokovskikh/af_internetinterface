using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Test.Unit
{
	public class LinksysCommutateurInfoTest : LinksysCommutateurInfo
	{
		public void GetInterfacesInfoTest(string interfaces, IDictionary propertyBag)
		{
			GetInterfacesInfo(interfaces, propertyBag);
		}

		public void GetCountersInfoTest(string counters, IDictionary propertyBag)
		{
			GetCountersInfo(counters, propertyBag);
		}

		public void GetSnoopingInfoTest(string[] macInfo, IDictionary propertyBag)
		{
			GetSnoopingInfo(macInfo, propertyBag);
		}
	}

	[TestFixture]
	public class LinksysCommutateurInfoFixture
	{
		private LinksysCommutateurInfoTest _info = new LinksysCommutateurInfoTest();
		private Dictionary<string, object> propertyBag = new Dictionary<string, object>();
			
		[Test]
		public void GetInterfacesInfoTest()
		{
			var test = @"*******



Dorogneimyg2>terminal length 0

% Unrecognized command
Dorogneimyg2>
                                             Flow Link          Back   Mdix
Port     Type         Duplex  Speed Neg      ctrl State       Pressure Mode
-------- ------------ ------  ----- -------- ---- ----------- -------- -------
fa1      100M-Copper    --      --     --     --  Down           --     --    ";
			_info.GetInterfacesInfoTest(test, propertyBag);
			var info = ((List<string[]>)propertyBag["interfaceLines"]);
			Assert.AreEqual(info.Count, 4);
			Assert.IsTrue(info.All(i => i.Length == 9));
		}

		[Test]
		public void GetCountersInfoTest()
		{
			var test = @"

\r
      Port       InUcastPkts  InMcastPkts  InBcastPkts    InOctets   
---------------- ------------ ------------ ------------ ------------ 
      fa1          28630863      78556         1828      2950253549  

      Port       OutUcastPkts OutMcastPkts OutBcastPkts  OutOctets   
---------------- ------------ ------------ ------------ ------------ 
      fa1          46049005       6008        588954    50409977757  

Alignment Errors: 0
FCS Errors: 0
Single Collision Frames: 0
Multiple Collision Frames: 0
SQE Test Errors: 0
Deferred Transmissions: 0
Late Collisions: 0
Excessive Collisions: 0
Carrier Sense Errors: 0
Oversize Packets: 0
Internal MAC Rx Errors: 5
Symbol Errors: 0
Received Pause Frames: 0";
			_info.GetCountersInfoTest(test, propertyBag);
			var info = ((List<string[]>)propertyBag["countersLines"]);
			Assert.AreEqual(info.Count, 6);
			Assert.IsTrue(info.All(i => i.Length == 5));
		}

		[Test]
		public void GetSnoopingInfoTest()
		{
			var array = new string[25];
			array[20] = "MAC";
			array[21] = "IP";
			_info.GetSnoopingInfoTest(array, propertyBag);
			Assert.AreEqual(propertyBag["IPResult"], "IP");
			Assert.AreEqual(propertyBag["MACResult"], "MAC");
			array = new string[15];
			_info.GetSnoopingInfoTest(array, propertyBag);
			Assert.IsTrue(propertyBag["Message"] is Message);
		}
	}
}
