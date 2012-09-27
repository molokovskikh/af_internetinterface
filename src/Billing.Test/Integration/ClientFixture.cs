using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Castle.ActiveRecord;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class ClientFixture : MainBillingFixture
	{
		[Test]
		public void Payment_for_connect_fixture()
		{
			using (new TransactionScope(OnDispose.Rollback)) {
				ArHelper.WithSession(s => {
					_client.BeginWork = null;
					s.SaveOrUpdate(_client);
					var clientEndPoint = new ClientEndpoint { Client = _client };
					var paymentForConnect = new PaymentForConnect(500, clientEndPoint);
					_client.PhysicalClient.Balance = 100;
					s.Save(clientEndPoint);
					s.Save(paymentForConnect);
					s.SaveOrUpdate(_client.PhysicalClient);
				});
			}
			billing.OnMethod();
			ArHelper.WithSession(s => {
				_client = s.Get<Client>(_client.Id);
				Assert.IsNull(_client.BeginWork);
				Assert.AreEqual(_client.Balance, 200);
				_client.BeginWork = DateTime.Now;
				s.SaveOrUpdate(_client);
			});
			billing.OnMethod();
			ArHelper.WithSession(s => {
				_client = s.Get<Client>(_client.Id);
				Assert.IsNotNull(_client.BeginWork);
				Assert.AreEqual(_client.Balance, -300);
				var userWtiteOffs = _client.UserWriteOffs.First();
				Assert.AreEqual(userWtiteOffs.Sum, 500);
				Assert.AreEqual(userWtiteOffs.Comment, "Плата за подключение");
				Assert.AreEqual(_client.UserWriteOffs.Count, 1);
			});
		}
	}
}
