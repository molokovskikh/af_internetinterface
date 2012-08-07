using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Castle.ActiveRecord;
using InternetInterface.Helpers;
using InternetInterface.Models;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class SmsFixture : IntegrationFixture
	{
		[Test]
		public void DeleteNoSendingTest()
		{
			var client = new Client();
			client.Save();
			session.Flush();
			new SmsMessage(client, "test_message").Save();
			session.Flush();
			Assert.That(SmsMessage.Queryable.Count(m => m.Client == client), Is.GreaterThan(0));
			SmsHelper.DeleteNoSendingMessages(client);
			Assert.That(SmsMessage.Queryable.Count(m => m.Client == client), Is.EqualTo(0));
		}
	}
}
