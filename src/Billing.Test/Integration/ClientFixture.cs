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
		public void VirtualPaymentAndUnBlockClient()
		{
			InitSession();

			client.PhysicalClient.Balance = -30;
			client.PhysicalClient.MoneyBalance = -30;
			client.PhysicalClient.VirtualBalance = -30;
			session.Update(client.PhysicalClient);
			client.PhysicalClient.Tariff.Price = 1000;
			session.Update(client.PhysicalClient.Tariff);
			client.Disabled = true;
			client.PercentBalance = 0.8m;
			session.Update(client);
			var payment = new Payment(client, 100) {
				Virtual = true
			};
			session.Save(payment);
			billing.OnMethod();
			session.Refresh(client);
			Assert.IsFalse(client.Disabled);
		}
	}
}
