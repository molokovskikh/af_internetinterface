﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using Common.Tools;
using Common.Tools.Calendar;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Models;
using InternetInterface.Models;
using NHibernate.Linq;
using NHibernate.Mapping;
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
			//В тесте эти данные не используются (и кстати зря)
			//но биллинг без них упадет, так как пошлет на почту письмо с номером заявки
			var request = new ServiceRequest();
			request.Client = client;
			request.Description = "test test test";
			session.Save(request);
			var region = new RegionHouse("Воронеж");
			session.Save(region);
			region = new RegionHouse("Белгород");
			session.Save(region);
			var house = new House("dsadasd", 11, region);
			session.Save(house);

			client.RatedPeriodDate = DateTime.Now;
			client.SetStatus(StatusType.BlockedForRepair, session);
			client.PhysicalClient.HouseObj = house;
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

		[Test(Description = "Разблокировка юр. лиц при положительном балансе")]
		public void UnBlock_lawyer_person_negative_balance()
		{
			//Более мнее красивый тест для юр. лиц
			//Можно будет потом переписать работу с юр. лицами
			var region = new RegionHouse
			{
				Name = "Воронеж"
			};
			session.Save(region);
			var status = session.Load<Status>((uint)StatusType.Worked);

			//Клиент с отрицательным балансом
			var BadPerson = new LawyerPerson
			{
				Balance = -3000,
				Region = region,
			};

			session.Save(BadPerson);
			var BadClient = new Client()
			{
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

			//Тест
			Assert.That(BadClient.Disabled, Is.False);
			billing.ProcessWriteoffs();
			var saved = session.Load<Client>(BadClient.Id);
			Assert.That(saved.Disabled, Is.True);

			var payment = new Payment(saved, 10000);
			session.Save(payment);

			billing.ProcessPayments();
			saved = session.Load<Client>(BadClient.Id);
			Assert.That(saved.Disabled, Is.False);

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
			var GoodPerson = new LawyerPerson {
				Balance = 3000,
				Region = region,
			};
			session.Save(GoodPerson);
			var GoodClient = new Client() {
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
			Assert.That(BadClient.Disabled, Is.False);
			Assert.That(GoodClient.Disabled, Is.False);
			billing.ProcessWriteoffs();
			var saved = session.Load<Client>(BadClient.Id);
			var saved2 = session.Load<Client>(GoodClient.Id);
			Assert.That(saved.Disabled, Is.True);
			Assert.That(saved2.Disabled, Is.False);
		}

		[Test(Description = "Начисление начисления бонусов физикам за первый (основной) платеж")]
		public void first_payment_bonus()
		{
			ConfigurationManager.AppSettings["ProcessFirstPaymentBonus"] = "1";
			client.PhysicalClient.Balance = 0;
			client.PhysicalClient.MoneyBalance = 0;
			client.PhysicalClient.VirtualBalance = 0;
			session.Update(client.PhysicalClient);
			client.PhysicalClient.Tariff.Name = "Популярный";		// Пример для теста
			client.PhysicalClient.Tariff.Price = 500;
			session.Update(client.PhysicalClient.Tariff);

			// Изменим Id тарифного плана "Популярный" в биллинге (на время теста)
			billing.FirstPaymentBonusTariffIds[0] = client.PhysicalClient.Tariff.Id;

			//Создадим платеж
			var payment = new Payment(client, 600) {
				Virtual = true
			};
			session.Save(payment);

			//эта строчка нужна, так как иначе биллинг получит клиента без платежа - сессия не отдельная, однако
			session.Refresh(client);
			Assert.That(client.Balance, Is.EqualTo(0));
			Assert.That(client.Payments.Count, Is.EqualTo(1));
			Assert.That(client.Payments[0].Comment, Is.EqualTo(null));
			//Проверяем 1 раз - новый платеж был создан, но не обработан
			billing.ProcessPayments();
			session.Refresh(client);
			Assert.That(client.Balance, Is.EqualTo(600));
			Assert.That(client.Payments.Count, Is.EqualTo(2));
			Assert.That(client.Payments[1].Comment, Is.EqualTo("Месяц в подарок"));
			//Во второй раз обработается виртуальный бонусный платеж
			billing.ProcessPayments();
			session.Refresh(client);
			Assert.That(client.Balance, Is.EqualTo(1100));

			//Создадим еще платеж
			payment = new Payment(client, 600)
			{
				Virtual = true
			};
			session.Save(payment);
			session.Refresh(client);
			Assert.That(client.Payments.Count, Is.EqualTo(3));

			//Пусть билинг хоть заработается себе, но никаких новых бонусов быть не должно
			billing.ProcessPayments();
			billing.ProcessPayments();
			billing.ProcessPayments();
			Assert.That(client.Balance, Is.EqualTo(1700));

			ConfigurationManager.AppSettings["ProcessFirstPaymentBonus"] = null;
		}

		[Test(Description = "Проверка начисления бонуса при быстром приходе 2-го платежа; тест для задачи 30619")]
		public void Check_first_bonus_before_2nd_payment()
		{
			ConfigurationManager.AppSettings["ProcessFirstPaymentBonus"] = "1";

			client.Name = "Billing_client_with_2_payments";
			client.PhysicalClient.Balance = 0;
			client.PhysicalClient.MoneyBalance = 0;
			client.PhysicalClient.VirtualBalance = 0;
			session.Update(client.PhysicalClient);
			client.PhysicalClient.Tariff.Name = "Оптимальный";	// Пример для теста
			client.PhysicalClient.Tariff.Price = 500;
			session.Update(client.PhysicalClient.Tariff);

			// Изменим Id тарифного плана "Оптимальный" в биллинге (на время теста)
			billing.FirstPaymentBonusTariffIds[1] = client.PhysicalClient.Tariff.Id;

			// Создание двух платежей для клиента
			var payment1 = new Payment(client, 600) {
				PaidOn = DateTime.Now,
				RecievedOn = DateTime.Now,
				Comment = "payment1",
				Virtual = true
			};
			session.Save(payment1);
			var payment2 = new Payment(client, 600) {
				PaidOn = DateTime.Now.AddSeconds(30),							// Для формального соблюдения паузы при оплате
				RecievedOn = DateTime.Now.AddSeconds(30),					// Для формального соблюдения паузы при оплате 
				Comment = "payment2",
				Virtual = true
			};
			session.Save(payment2);
			client.Refresh();

			// 1-я обработка платежей (бонус только внесен)
			billing.ProcessPayments();
			client.Refresh();
			Assert.That(client.Payments.Count, Is.EqualTo(3));	// payment1, payment2, bonus
			Assert.That(client.Balance, Is.EqualTo(1200));			// 600 (payment1) + 600 (payment2)

			// 2-я обработка платежей (бонус обработан)
			billing.ProcessPayments();
			client.Refresh();
			Assert.That(client.Balance, Is.EqualTo(1700));			// 600 (payment1) + 600 (payment2) + 500 (bonus)

			ConfigurationManager.AppSettings["ProcessFirstPaymentBonus"] = null;
		}

		[Test(Description = "Проверка неначисления бонуса в случае прихода первого НЕосновного платежа; тест для задачи 30871")]
		public void Check_first_payment_without_bonus()
		{
			ConfigurationManager.AppSettings["ProcessFirstPaymentBonus"] = "1";

			client.Name = "Billing_client_with_payment_without_bonus";
			client.PhysicalClient.Balance = 0;
			client.PhysicalClient.MoneyBalance = 0;
			client.PhysicalClient.VirtualBalance = 0;
			session.Update(client.PhysicalClient);
			client.PhysicalClient.Tariff.Name = "Лёгкий";				// Пример для теста
			client.PhysicalClient.Tariff.Price = 500;
			session.Update(client.PhysicalClient.Tariff);

			// Изменим Id тарифного плана, совпадающего по своему Id в биллинге (на время теста)
			var tariff = client.PhysicalClient.Tariff;
			var index = billing.FirstPaymentBonusTariffIds.IndexOf(tariff.Id);
			if (index > 0)
				billing.FirstPaymentBonusTariffIds[index] = client.PhysicalClient.Tariff.Id;

			// Создание платежей для клиента
			var payment = new Payment(client, 600) {
				PaidOn = DateTime.Now,
				RecievedOn = DateTime.Now,
				Comment = "payment",
				Virtual = true
			};
			session.Save(payment);
			client.Refresh();

			// 1-я обработка платежей (платеж внесен без бонуса)
			billing.ProcessPayments();
			client.Refresh();
			Assert.That(client.Payments.Count, Is.EqualTo(1));	// payment
			Assert.That(client.Balance, Is.EqualTo(600));				// 600 (payment)

			// 2-я обработка платежей (бонус не начислен)
			billing.ProcessPayments();
			client.Refresh();
			Assert.That(client.Balance, Is.EqualTo(600));				// 600 (payment) + 0 (bonus)

			ConfigurationManager.AppSettings["ProcessFirstPaymentBonus"] = null;
		}
	}
}