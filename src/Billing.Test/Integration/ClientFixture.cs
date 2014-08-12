using System;
using System.Linq;
using Common.Tools;
using InternetInterface.Models;
using NHibernate.Linq;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	[TestFixture]
	public class ClientFixture : BillingFixture2
	{
		[Test]
		public void Payment_for_connect_fixture()
		{
			client.BeginWork = null;
			session.Save(client);
			var clientEndPoint = new ClientEndpoint { Client = client };
			var paymentForConnect = new PaymentForConnect(500, clientEndPoint);
			client.PhysicalClient.Balance = 200;
			session.Save(clientEndPoint);
			session.Save(paymentForConnect);
			session.Save(client.PhysicalClient);

			session.Flush();
			session.Clear();
			billing.ProcessPayments();
			session.Flush();
			session.Clear();

			client = session.Get<Client>(client.Id);
			Assert.IsNull(client.BeginWork);
			Assert.AreEqual(client.Balance, 200);
			client.BeginWork = DateTime.Now;
			session.Save(client);

			session.Flush();
			session.Clear();
			billing.ProcessPayments();
			session.Flush();
			session.Clear();

			client = session.Get<Client>(client.Id);
			Assert.IsNotNull(client.BeginWork);
			Assert.AreEqual(client.Balance, -300);
			var userWtiteOffs = client.UserWriteOffs.First();
			Assert.AreEqual(userWtiteOffs.Sum, 500);
			Assert.AreEqual(userWtiteOffs.Comment, "Плата за подключение");
			Assert.AreEqual(client.UserWriteOffs.Count, 1);
		}

		[Test]
		public void Payment_for_connect_lawyer_person()
		{
			Assert.IsNotNull(client.BeginWork);
			var lawPerson = new LawyerPerson();
			lawPerson.Region = session.Query<RegionHouse>().FirstOrDefault();
			session.Save(lawPerson);
			client.PhysicalClient = null;
			client.LawyerPerson = lawPerson;
			session.SaveOrUpdate(client);
			var clientEndPoint = new ClientEndpoint { Client = client };
			var paymentForConnect = new PaymentForConnect(500, clientEndPoint);
			session.Save(clientEndPoint);
			session.Save(paymentForConnect);

			session.Flush();
			session.Clear();
			billing.ProcessPayments();
			session.Flush();
			session.Clear();

			client = session.Get<Client>(client.Id);
			Assert.AreEqual(client.UserWriteOffs.Count, 0);
		}

		[Test]
		public void VirtualPaymentAndUnBlockClient()
		{
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
			billing.ProcessPayments();
			session.Refresh(client);
			Assert.IsFalse(client.Disabled);
		}

		[Test]
		public void Reset_repair_status_on_timeout()
		{
			client.RatedPeriodDate = DateTime.Now;
			client.SetStatus(StatusType.BlockedForRepair, session);
			session.Save(client);
			session.Flush();
			session.Transaction.Commit();
			billing.ProcessPayments();
			billing.ProcessWriteoffs();

			session.Clear();
			client = session.Load<Client>(client.Id);
			Assert.AreEqual(0, client.WriteOffs.Count);

			SystemTime.Now = () => DateTime.Now.AddDays(2);
			billing.ProcessPayments();
			billing.ProcessWriteoffs();

			session.Clear();
			client = session.Load<Client>(client.Id);
			Assert.AreEqual(StatusType.BlockedForRepair, client.Status.Type);
			//симулируем обращение к dhcp
			session.Save(client);
			session.Flush();

			SystemTime.Now = () => DateTime.Now.AddDays(3);
			billing.ProcessPayments();
			billing.ProcessWriteoffs();

			//если прошло три дня ставим статус работает, но денег не списываем тк возможности работать не было
			session.Clear();
			client = session.Load<Client>(client.Id);
			Assert.AreEqual(StatusType.Worked, client.Status.Type);
			Assert.AreEqual(0, client.WriteOffs.Count);

			SystemTime.Now = () => DateTime.Now.AddDays(4);
			billing.ProcessPayments();
			billing.ProcessWriteoffs();

			//на четвертый день списываем деньги
			session.Clear();
			client = session.Load<Client>(client.Id);
			Assert.AreEqual(StatusType.Worked, client.Status.Type);
			Assert.AreEqual(1, client.WriteOffs.Count);
		}
	}
}
