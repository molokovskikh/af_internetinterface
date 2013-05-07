using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class ClientFixture : MainBillingFixture
	{
		[Test]
		public void Payment_for_connect_fixture()
		{
			using (new TransactionScope(OnDispose.Commit)) {
				ArHelper.WithSession(s => {
					client.BeginWork = null;
					s.SaveOrUpdate(client);
					var clientEndPoint = new ClientEndpoint { Client = client };
					var paymentForConnect = new PaymentForConnect(500, clientEndPoint);
					client.PhysicalClient.Balance = 200;
					s.Save(clientEndPoint);
					s.Save(paymentForConnect);
					s.SaveOrUpdate(client.PhysicalClient);
				});
			}
			billing.OnMethod();
			using (new TransactionScope(OnDispose.Commit)) {
				ArHelper.WithSession(s => {
					client = s.Get<Client>(client.Id);
					Assert.IsNull(client.BeginWork);
					Assert.AreEqual(client.Balance, 200);
					client.BeginWork = DateTime.Now;
					s.SaveOrUpdate(client);
				});
			}
			billing.OnMethod();
			using (new TransactionScope(OnDispose.Commit)) {
				ArHelper.WithSession(s => {
					client = s.Get<Client>(client.Id);
					Assert.IsNotNull(client.BeginWork);
					Assert.AreEqual(client.Balance, -300);
					var userWtiteOffs = client.UserWriteOffs.First();
					Assert.AreEqual(userWtiteOffs.Sum, 500);
					Assert.AreEqual(userWtiteOffs.Comment, "Плата за подключение");
					Assert.AreEqual(client.UserWriteOffs.Count, 1);
				});
			}
		}

		[Test]
		public void Payment_for_connect_lawyer_person()
		{
			using (new TransactionScope(OnDispose.Commit)) {
				ArHelper.WithSession(s => {
					Assert.IsNotNull(client.BeginWork);
					var lawPerson = new LawyerPerson();
					lawPerson.Region = s.Query<RegionHouse>().FirstOrDefault();
					s.Save(lawPerson);
					client.PhysicalClient = null;
					client.LawyerPerson = lawPerson;
					s.SaveOrUpdate(client);
					var clientEndPoint = new ClientEndpoint { Client = client };
					var paymentForConnect = new PaymentForConnect(500, clientEndPoint);
					s.Save(clientEndPoint);
					s.Save(paymentForConnect);
				});
			}
			billing.OnMethod();
			using (new TransactionScope(OnDispose.Commit)) {
				ArHelper.WithSession(s => {
					client = s.Get<Client>(client.Id);
					Assert.AreEqual(client.UserWriteOffs.Count, 0);
				});
			}
		}

		[Test]
		public void StaticIpFixture()
		{
			using (new SessionScope()) {
				var networkSwitch = new NetworkSwitch();
				ActiveRecordMediator.Save(networkSwitch);
				var endPoint = new ClientEndpoint(client, 10, networkSwitch);
				client.Endpoints.Add(endPoint);
				ActiveRecordMediator.Save(endPoint);
				client.PhysicalClient.Balance = 0;
				ActiveRecordMediator.Save(client);
				ActiveRecordMediator.Save(client.PhysicalClient);
				endPoint.Ip = new IPAddress(1541080065);
				ActiveRecordMediator.Save(endPoint);
			}
			billing.Compute();
			using (new SessionScope()) {
				ArHelper.WithSession(s => {
					client = s.Get<Client>(client.Id);
					Assert.AreEqual(client.Disabled, true);
					Assert.IsTrue(client.Endpoints[0].Ip.Equals(new IPAddress(1541080065)));
					Assert.AreEqual(client.BlockDate.Value.Date, DateTime.Now.Date);
				});
			}
			SystemTime.Now = () => DateTime.Now.AddDays(62);
			billing.Compute();
			using (new SessionScope()) {
				ArHelper.WithSession(s => {
					client = s.Get<Client>(client.Id);
					Assert.AreEqual(client.Disabled, true);
					Assert.IsNull(client.Endpoints[0].Ip);
					Assert.IsNull(client.BlockDate);
				});
			}
		}
	}
}
