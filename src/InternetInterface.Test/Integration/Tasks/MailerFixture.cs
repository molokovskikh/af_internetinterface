using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.NHibernateExtentions;
using InternetInterface.Background;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration.Tasks
{
	[TestFixture]
	public class MailerFixture : IntegrationFixture
	{
		[Test]
		public void SmsTest()
		{
			SmsMessage.DeleteAll();

			var messages = new List<SmsMessage> {
				new SmsMessage { PhoneNumber = "123456789" },
				new SmsMessage {
					IsSended = true,
					PhoneNumber = "123456789"
				},
				new SmsMessage { PhoneNumber = "123456789" },
				new SmsMessage {
					IsSended = true,
					PhoneNumber = "123456789"
				}
			};
			session.SaveMany(messages.ToArray());
			var sent = new SendSmsNotification(session).SendMessages();
			Assert.That(sent.Count, Is.EqualTo(2));
		}

		[Test]
		public void LawyerTest()
		{
			new SendNullTariffLawyerPerson(session).Execute();
		}

		[Test]
		public void StaticIpFixture()
		{
			var client = ClientHelper.Client();
			ActiveRecordMediator.Save(client);
			var networkSwitch = new NetworkSwitch();
			ActiveRecordMediator.Save(networkSwitch);
			var endPoint = new ClientEndpoint(client, 10, networkSwitch);
			client.Endpoints.Add(endPoint);
			ActiveRecordMediator.Save(endPoint);
			client.PhysicalClient.Balance = 0;
			client.Disabled = true;
			ActiveRecordMediator.Save(client);
			ActiveRecordMediator.Save(client.PhysicalClient);
			endPoint.Ip = new IPAddress(1541080065);
			ActiveRecordMediator.Save(endPoint);

			new DeleteFixIpIfClientLongDisable(session).Execute();

			client = session.Get<Client>(client.Id);
			Assert.AreEqual(client.Disabled, true);
			Assert.IsTrue(client.Endpoints[0].Ip.Equals(new IPAddress(1541080065)));
			Assert.AreEqual(client.BlockDate.Value.Date, DateTime.Now.Date);
			SystemTime.Now = () => DateTime.Now.AddDays(61);

			new DeleteFixIpIfClientLongDisable(session).Execute();

			client = session.Get<Client>(client.Id);
			Assert.AreEqual(client.Disabled, true);
			Assert.IsNull(client.Endpoints[0].Ip);
			Assert.IsNull(client.BlockDate);
		}

		[Test]
		public void BaseTest()
		{
			var unknownLease = new Lease {
				LeaseBegin = DateTime.Now,
				LeaseEnd = DateTime.Now.AddHours(5),
				LeasedTo = "14-D6-4D-38-07-2F-00-00-00-00-00-00-00-00-00-00",
				Port = 5,
				Pool = IpPool.FindFirst(),
				Switch = ActiveRecordLinqBase<NetworkSwitch>.FindFirst(),
				Ip = new IPAddress(3541660034)
			};
			unknownLease.Save();

			new SendUnknowEndPoint(session).Execute();

			var sendedLease = session.Query<SendedLease>().FirstOrDefault(s => s.LeaseId == unknownLease.Id);
			Assert.IsNotNull(sendedLease);

			new SendUnknowEndPoint(session).Execute();

			var sendedLeases = session.Query<SendedLease>().Where(s => s.LeaseId == unknownLease.Id).ToList();
			Assert.LessOrEqual(1, sendedLeases.Count);
			unknownLease.Delete();
		}
	}
}