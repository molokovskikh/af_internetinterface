using System;
using System.Collections.Generic;
using System.Linq;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.ActiveRecordExtentions;
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

			Process();

			client = session.Get<Client>(client.Id);
			Assert.IsNull(client.BeginWork);
			Assert.AreEqual(client.Balance, 200);
			client.BeginWork = DateTime.Now;
			session.Save(client);

			Process();

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

			Process();

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
			Process();

			client = session.Load<Client>(client.Id);
			Assert.AreEqual(0, client.WriteOffs.Count);

			Process(2.Days());

			client = session.Load<Client>(client.Id);
			Assert.AreEqual(StatusType.BlockedForRepair, client.Status.Type);

			//если прошло три дня ставим статус работает, но денег не списываем тк возможности работать не было
			Process(3.Days());

			client = session.Load<Client>(client.Id);
			Assert.AreEqual(StatusType.Worked, client.Status.Type);
			Assert.AreEqual(0, client.WriteOffs.Count);

			//на четвертый день списываем деньги
			Process(4.Days());

			client = session.Load<Client>(client.Id);
			Assert.AreEqual(StatusType.Worked, client.Status.Type);
			Assert.AreEqual(1, client.WriteOffs.Count);
		}

		[Test(Description = "При обработке платежа происходит установка флага автоматической разблокировки, " +
			"в случае восстановления работы этого происходить не должно")]
		public void Do_not_auto_unblock_on_payment()
		{
			client.RatedPeriodDate = DateTime.Now;
			client.SetStatus(StatusType.BlockedForRepair, session);
			session.Save(client);
			session.Save(new Payment(client, 500));

			Process();

			client = session.Load<Client>(client.Id);
			Assert.AreEqual(StatusType.BlockedForRepair, client.Status.Type);
		}

		private void Process(TimeSpan? offset = null)
		{
			if (offset != null)
				SystemTime.Now = () => DateTime.Now.Add(offset.Value);

			session.Flush();
			if (session.Transaction.IsActive)
				session.Transaction.Commit();
			//в результате работы биллинга объекты могут быть удалены например ClientService
			//если сделать Refresh для такого объекта в коллекции которого есть такой объект то это приведет к ошибке
			session.Clear();
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
		}

		[Test(Description = "Блокировка юр. лиц при негативном балансе")]
		public void Block_lawyer_person_negative_balance()
		{
			//Более мнее красивый тест для юр. лиц
			//Можно будет потом переписать работу с юр. лицами
			var region = new RegionHouse {
						Name = "Воронеж"
					};
			session.Save(region);
			var status = session.Load<Status>((uint)StatusType.Worked);

			//Клиент с отрицательным балансом
			var BadPerson = new LawyerPerson {
				Balance = -3000,
				Region = region,
			};

			session.Save(BadPerson);
			var BadClient = new Client() {
				Disabled = false,
				Name = "TestLawyer",
				ShowBalanceWarningPage = false,
				LawyerPerson = BadPerson,
				Status = status
			};
			session.Save(BadClient);

			var order = new Order() { BeginDate = DateTime.Now, Client = BadClient, OrderServices = new List<OrderService>() };
			var service = new OrderService() { Cost = 100, IsPeriodic = true, Description = "testService", Order = order };
			order.OrderServices.Add(service);
			BadClient.Orders = new List<Order>();
			BadClient.Orders.Add(order);
			session.Save(service);
			session.Save(order);
			BadPerson.client = BadClient;
			session.Save(BadPerson);

			//Клиент с положительным балансом
			var GoodPerson = new LawyerPerson
			{
				Balance = 3000,
				Region = region,
			};
			session.Save(GoodPerson);
			var GoodClient = new Client()
			{
				Disabled = false,
				Name = "TestLawyer2",
				ShowBalanceWarningPage = false,
				LawyerPerson = GoodPerson,
				Status = status
			};
			session.Save(GoodClient);
			GoodPerson.client = GoodClient;
			session.Save(GoodPerson);

			//Сам тест
			Assert.That(BadClient.Disabled,Is.False);
			Assert.That(GoodClient.Disabled,Is.False);
			billing.ProcessWriteoffs();
			var saved = session.Load<Client>(BadClient.Id);
			var saved2 = session.Load<Client>(GoodClient.Id);
			Assert.That(saved.Disabled, Is.True);
			Assert.That(saved2.Disabled, Is.False);
		}
	}
}
