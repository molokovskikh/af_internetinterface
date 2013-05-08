using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Background;
using InternetInterface.Models;
using InternetInterface.Test.Helpers;
using NUnit.Framework;
using Test.Support.log4net;

namespace InternetInterface.Test.Integration.Tasks
{
	[TestFixture]
	public class MailerFixture
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
			using (new SessionScope()) {
				foreach (var smsMessage in messages) {
					smsMessage.Save();
				}
				var sededMessages = SendProcessor.SendSmsNotification();
				Assert.That(sededMessages.Count, Is.EqualTo(2));
			}
		}

		[Test]
		public void LawyerTest()
		{
			using (new SessionScope()) {
				SendProcessor.SendNullTariffLawyerPerson();
			}
		}

		[Test]
		public void StaticIpFixture()
		{
			Client client;
			using (new SessionScope()) {
				client = ClientHelper.Client();
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
				SendProcessor.DeleteFixIpIfClientLongDisable();
				ArHelper.WithSession(s => {
					client = s.Get<Client>(client.Id);
					Assert.AreEqual(client.Disabled, true);
					Assert.IsTrue(client.Endpoints[0].Ip.Equals(new IPAddress(1541080065)));
					Assert.AreEqual(client.BlockDate.Value.Date, DateTime.Now.Date);
				});
				SystemTime.Now = () => DateTime.Now.AddDays(61);
				SendProcessor.DeleteFixIpIfClientLongDisable();
				ArHelper.WithSession(s => {
					client = s.Get<Client>(client.Id);
					Assert.AreEqual(client.Disabled, true);
					Assert.IsNull(client.Endpoints[0].Ip);
					Assert.IsNull(client.BlockDate);
				});
			}
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
					Pool = IpPool.FindFirst(),
					Switch = ActiveRecordLinqBase<NetworkSwitch>.FindFirst(),
					Ip = new IPAddress(3541660034)
				};
				unknownLease.Save();
			}

			SendProcessor.Process();

			using (new SessionScope()) {
				var sendedLease = SendedLease.Queryable.FirstOrDefault(s => s.LeaseId == unknownLease.Id);
				Assert.IsNotNull(sendedLease);
			}

			SendProcessor.Process();

			using (new SessionScope()) {
				var sendedLease = SendedLease.Queryable.Where(s => s.LeaseId == unknownLease.Id).ToList();
				Assert.LessOrEqual(1, sendedLease.Count);
				unknownLease.Delete();
			}
		}
	}
}