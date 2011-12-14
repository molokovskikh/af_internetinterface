﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Billing;
using Castle.ActiveRecord;
using InternetInterface.Models;
using NUnit.Framework;

namespace InternetInterface.Background.Tests
{
	[TestFixture]
	public class MailerFixture
	{
		[SetUp]
		public void Setup()
		{
			MainBilling.InitActiveRecord();
		}

		[Test]
		public void BaseTest()
		{
			Lease unknownLease;
			using (new SessionScope()) {
				unknownLease = new Lease {
					LeaseBegin = DateTime.Now,
					LeaseEnd = DateTime.Now.AddHours(5),
					LeasedTo = "14-D6-4D-38-07-2F-00-00-00-00-00-00-00-00-00-00",
					Port = 5,
					Pool = 1,
					Module = 1,
					Switch = NetworkSwitches.FindFirst(),
					Ip = 3541660034
				};
				unknownLease.Save();
			}

			new MailEndpointProcessor().Process();

			using (new SessionScope()) {
				var sendedLease = SendedLease.Queryable.Where(s => s.LeaseId == unknownLease.Id).FirstOrDefault();
				Assert.IsNotNull(sendedLease);
			}

			new MailEndpointProcessor().Process();

			
			using (new SessionScope()) {
				var sendedLease = SendedLease.Queryable.Where(s => s.LeaseId == unknownLease.Id).ToList();
				Assert.LessOrEqual(1, sendedLease.Count);
				unknownLease.Delete();
			}
		}
	}
}