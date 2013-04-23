﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework.Scopes;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Impl;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support.log4net;

namespace Billing.Test.Integration
{
	public class ServiceFixture : MainBillingFixture
	{
		private ClientService Activate(Type type, DateTime? endDate = null)
		{
			_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);

			if (endDate == null)
				endDate = DateTime.Now.AddDays(1);

			var service = new ClientService {
				Client = _client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = endDate.Value,
				Service = Service.GetByType(type)
				//Activator = InitializeContent.Partner
			};

			_client.Activate(service);

			ActiveRecordMediator.Save(_client);

			return service;
		}

		[Test]
		public void VolBlockIfMinimumBalance()
		{
			using (new SessionScope()) {
				_client.FreeBlockDays = 28;
				_client.PhysicalClient.Balance = _client.GetPrice() / _client.GetInterval() - 5;
				_client.Save();
				Assert.That(_client.PhysicalClient.Balance, Is.GreaterThan(0));

				Activate(typeof(VoluntaryBlockin));

				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				Assert.IsTrue(_client.PaidDay);
			}

			billing.Compute();
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.Compute();

			using (new SessionScope()) {
				_client.Refresh();
				Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(1));
				Assert.That(WriteOff.Queryable.Count(w => w.Client == _client), Is.EqualTo(0));
				Assert.IsFalse(_client.PaidDay);
			}
		}

		[Test]
		public void Do_not_deactivate_voluntary_blocking_on_payment()
		{
			using (new SessionScope())
				Activate(typeof(VoluntaryBlockin));

			new Payment(_client, 500).Save();

			billing.OnMethod();

			using (new SessionScope()) {
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				Assert.That(_client.Disabled, Is.True);
			}
		}

		[Test]
		public void Year_circle_date()
		{
			using (new SessionScope()) {
				_client.YearCycleDate = null;
				_client.BeginWork = null;
				ActiveRecordMediator.Save(_client);
			}
			billing.Compute();
			using (new SessionScope()) {
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				Assert.IsNull(_client.YearCycleDate);
				_client.BeginWork = DateTime.Now;
				ActiveRecordMediator.Save(_client);
			}
			billing.Compute();
			using (new SessionScope()) {
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				Assert.IsNotNull(_client.YearCycleDate);
			}
		}

		[Test]
		public void Free_days_test_vol_block()
		{
			ClientService service;
			using (new SessionScope()) {
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				_client.YearCycleDate = DateTime.Now.AddYears(-1);
			}
			billing.Compute();
			using (new SessionScope()) {
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				Assert.That(_client.FreeBlockDays, Is.EqualTo(MainBilling.FreeDaysVoluntaryBlockin));
				ArHelper.WithSession(s => { s.CreateSQLQuery("delete from Internet.WriteOff").ExecuteUpdate(); });
				Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(100));
			}
			using (new SessionScope()) {
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				service = _client.FindService<VoluntaryBlockin>();
				Assert.That(_client.FreeBlockDays, Is.EqualTo(MainBilling.FreeDaysVoluntaryBlockin));
				service.CompulsoryDeactivate();
				Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(1));
			}
			billing.Compute();
			using (new SessionScope()) {
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				var writeOffs = ActiveRecordMediator.FindAll(typeof(WriteOff)).Cast<WriteOff>().ToList();
				Assert.That(writeOffs.Count, Is.EqualTo(0), writeOffs.Implode());
				Assert.That(_client.FreeBlockDays, Is.EqualTo(MainBilling.FreeDaysVoluntaryBlockin));
			}
		}

		[Test]
		public void Decrement_freedays_vol_block()
		{
			using (new SessionScope()) {
				_client.FreeBlockDays = MainBilling.FreeDaysVoluntaryBlockin;
				ActiveRecordMediator.Save(_client);
				Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(100));
			}

			for (int i = 0; i < MainBilling.FreeDaysVoluntaryBlockin + 1; i++) {
				SystemTime.Now = () => DateTime.Now.AddDays(i);
				billing.Compute();
			}

			using (new SessionScope()) {
				Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(1), UserWriteOff.Queryable.Implode());
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				Assert.That(_client.FreeBlockDays, Is.EqualTo(0));
				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(0));
			}
		}

		[Test]
		public void If_free_block_days_zero()
		{
			ClientService service;
			using (new SessionScope()) {
				service = Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(100));
			}
			billing.Compute();
			using (new SessionScope())
				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(0));
			billing.Compute();
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(1));
				Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(2), UserWriteOff.Queryable.Implode());

				service = ActiveRecordMediator<ClientService>.FindByPrimaryKey(service.Id);
				service.CompulsoryDeactivate();
			}
			billing.Compute();
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(1));
			}
		}

		//Если не поперло, запусти 3 раза отдельно
		[Test]
		public void VoluntaryBlockingTest2()
		{
			using (new SessionScope()) {
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				var forRemove = _client.ClientServices.Where(s => NHibernateUtil.GetClass(s.Service) != typeof(Internet)).ToList();
				foreach (var assignedService in forRemove) {
					_client.ClientServices.Remove(assignedService);
				}
				ActiveRecordMediator.Save(_client);
			}
			using (new SessionScope()) {
				SystemTime.Reset();
				Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(4));
				Assert.That(ActiveRecordMediator<UserWriteOff>.FindAll().Length, Is.EqualTo(2));
			}
			for (int i = 0; i < 5; i++) {
				SystemTime.Now = () => DateTime.Now.AddDays(i);
				billing.OnMethod();
				billing.Compute();
			}
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(3));

				var userWriteOffs = UserWriteOff.Queryable.ToList();
				Assert.That(userWriteOffs.Count(), Is.EqualTo(3), userWriteOffs.Implode());
			}
		}

		[Test]
		public void DebtWorkTest()
		{
			const int countDays = 10;
			var client = _client;

			PhysicalClient physicalClient;
			ClientService CServive;

			using (new SessionScope()) {
				physicalClient = client.PhysicalClient;
				physicalClient.Balance = -10m;
				physicalClient.Update();
				client.Disabled = true;
				client.AutoUnblocked = true;
				client.RatedPeriodDate = SystemTime.Now();
				client.Update();

				CServive = new ClientService {
					Client = client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = SystemTime.Now().AddDays(countDays),
					Service = Service.GetByType(typeof(DebtWork)),
				};

				client.ClientServices.Add(CServive);
				CServive.Activate();
			}
			for (var i = 0; i < countDays; i++) {
				billing.OnMethod();
				billing.Compute();
				var i1 = i;
				SystemTime.Now = () => DateTime.Now.AddDays(i1 + 1);
			}
			SystemTime.Now = () => DateTime.Now.AddDays(countDays + 1);
			billing.OnMethod();
			using (new SessionScope()) {
				physicalClient = ActiveRecordMediator<PhysicalClient>.FindByPrimaryKey(physicalClient.Id);
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.That(WriteOff.FindAll().Count(), Is.EqualTo(countDays));
				Assert.That(physicalClient.Balance, Is.LessThan(0m));
				Assert.IsTrue(client.Disabled);
				client.Disabled = false;
				client.Update();
				//-50 из-за того, что платеж за 10 дней
				Assert.That(Math.Round(-client.GetPrice() / client.GetInterval() * countDays, 0) - 10 - 50,
					Is.EqualTo(Math.Round(physicalClient.Balance, 0)));
				Assert.That(client.RatedPeriodDate.Value.Date, Is.EqualTo(DateTime.Now.Date));
				CServive = new ClientService {
					Client = client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = SystemTime.Now().AddDays(countDays),
					Service = Service.GetByType(typeof(DebtWork)),
				};
				client.ClientServices.Add(CServive);

				client.Disabled = true;
				client.Update();
				new Payment {
					Client = client,
					Sum = client.PhysicalClient.Tariff.Price,
					PaidOn = DateTime.Now.AddDays(1)
				}.Save();
				SystemTime.Now = () => DateTime.Now.AddDays(countDays + 1);
			}
			billing.OnMethod();
			using (new SessionScope()) {
				physicalClient = ActiveRecordMediator<PhysicalClient>.FindByPrimaryKey(physicalClient.Id);
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				physicalClient.Balance = -10;
				physicalClient.Update();

				CServive = new ClientService {
					Client = client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = SystemTime.Now().AddDays(countDays),
					Service = Service.GetByType(typeof(DebtWork)),
				};
				client.ClientServices.Add(CServive);
				client.Disabled = true;
				ActiveRecordMediator.Save(client);

				CServive.Activate();
				Assert.That(CServive.Activated, Is.EqualTo(true));
			}
		}

		[Test]
		public void DebtWorkTestPartner()
		{
			using (new SessionScope()) {
				var client = CreateClient();
				const int countDays = 10;
				var physClient = client.PhysicalClient;
				physClient.Balance = -10m;
				physClient.Update();
				client.Disabled = true;
				client.AutoUnblocked = true;
				client.RatedPeriodDate = SystemTime.Now();
				client.Update();

				var cServive = new ClientService {
					Client = client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = SystemTime.Now().AddDays(countDays),
					Service = Service.GetByType(typeof(DebtWork)),
					Activator = InitializeContent.Partner
				};
				client.ClientServices.Add(cServive);

				cServive.Activate();
				Assert.That(cServive.Activated, Is.EqualTo(true));
				Assert.IsFalse(cServive.Client.Disabled);
				cServive.CompulsoryDeactivate();
				Assert.IsTrue(cServive.Client.Disabled);

				Assert.That(client.ClientServices.Count, Is.EqualTo(2), "должна остаться только услуга internet");
			}
		}

		[Test]
		public void Voluntary_blocking_and_minimum_balance()
		{
			decimal sum;
			using (new SessionScope()) {
				_client = CreateClient();
				_client.PhysicalClient.Balance = 5m;
				_client.AutoUnblocked = true;
				_client.FreeBlockDays = 0;
				_client.Save();
				var daysInInterval = _client.GetInterval();
				var price = _client.GetPrice();
				sum = price / daysInInterval;
			}
			using (new SessionScope()) {
				Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(7));
			}
			for (int i = 0; i < 5; i++) {
				billing.OnMethod();
				billing.Compute();
			}
			using (new SessionScope()) {
				_client = Client.Find(_client.Id);
				Assert.IsTrue(_client.Disabled);
				AssertThatContainsOnlyMandatoryServices();
				Assert.That(_client.PhysicalClient.Balance, Is.EqualTo(Math.Round(-50 - sum + 5, 2)), _client.UserWriteOffs.Implode());
			}
		}

		[Test]
		public void Debt_work_and_payment_whith_payment()
		{
			using (new SessionScope()) {
				_client = CreateClient();
				_client.PhysicalClient.Balance = -5m;
				_client.AutoUnblocked = true;
				_client.Disabled = true;
				_client.PercentBalance = 0.8m;
				_client.Save();
			}
			using (new SessionScope()) {
				Activate(typeof(DebtWork));
			}
			billing.OnMethod();
			billing.Compute();
			using (new SessionScope()) {
				_client = Client.Find(_client.Id);
				Assert.IsFalse(_client.Disabled);
				new Payment(_client, 100).Save();
			}
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.OnMethod();
			billing.Compute();
			using (new SessionScope()) {
				_client = Client.Find(_client.Id);
				Assert.IsTrue(_client.Disabled);
			}
			using (new SessionScope()) {
				_client = CreateClient();
				_client.PhysicalClient.Balance = -5m;
				_client.AutoUnblocked = true;
				_client.Disabled = true;
				_client.PercentBalance = 0.8m;
				_client.Save();
				Activate(typeof(DebtWork));
			}
			using (new SessionScope()) {
				_client = Client.Find(_client.Id);
				Assert.IsFalse(_client.Disabled);
				new Payment(_client, _client.GetPriceForTariff()).Save();
			}
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.OnMethod();
			billing.Compute();
			using (new SessionScope()) {
				_client = Client.Find(_client.Id);
				Assert.IsFalse(_client.Disabled);
			}
		}

		[Test]
		public void VoluntaryBlockinTest()
		{
			int countDays = 10;
			Client client;
			ClientService service;
			using (new SessionScope()) {
				client = CreateClient();

				client.AutoUnblocked = true;
				client.FreeBlockDays = MainBilling.FreeDaysVoluntaryBlockin;
				client.Update();

				service = new ClientService {
					Client = client,
					Activator = InitializeContent.Partner,
					Service = Service.GetByType(typeof(VoluntaryBlockin)),
					BeginWorkDate = DateTime.Now.AddDays(2),
					EndWorkDate = DateTime.Now.AddDays(countDays + 2)
				};

				client.ClientServices.Add(service);
				service.Activate();
			}
			billing.OnMethod();
			billing.Compute();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsFalse(client.Disabled);
				//WriteOff.DeleteAll();
				ArHelper.WithSession(s => { s.CreateSQLQuery("delete from Internet.WriteOff").ExecuteUpdate(); });
				SystemTime.Now = () => DateTime.Now.AddDays(2);
			}
			billing.OnMethod();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsTrue(client.Disabled);
			}
			for (int i = 0; i < countDays; i++) {
				billing.OnMethod();
				billing.Compute();
				int i1 = i;
				SystemTime.Now = () => DateTime.Now.AddDays(i1 + 3);
			}
			using (new SessionScope())
				Assert.That(WriteOff.Queryable.Where(c => c.Client == client).ToList().Sum(w => w.WriteOffSum), Is.EqualTo(0));

			SystemTime.Now = () => DateTime.Now.AddDays(countDays + 3);
			billing.OnMethod();
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.IsFalse(client.Disabled);
				Assert.That(client.ClientServices.Count, Is.EqualTo(2), "должна остаться только услуга internet");
			}
			SystemTime.Now = () => service.EndWorkDate.Value.AddDays(46);
			billing.OnMethod();

			using (new SessionScope()) {
				client.Refresh();
				Assert.That(client.ClientServices.Count, Is.EqualTo(2), "должна остаться только услуга internet");
				Assert.IsFalse(client.Disabled);
			}

			SystemTime.Reset();
			var balance = 0m;
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				service = new ClientService {
					Client = client,
					Activator = InitializeContent.Partner,
					Service = Service.GetByType(typeof(VoluntaryBlockin)),
					BeginWorkDate = DateTime.Now,
					EndWorkDate = DateTime.Now.AddDays(300)
				};
				client.ClientServices.Add(service);
				service.Activate();
				client.Update();
				countDays = 0;

				client.FreeBlockDays = MainBilling.FreeDaysVoluntaryBlockin;
				client.Update();
				balance = client.PhysicalClient.Balance;
				//WriteOff.DeleteAll();
				ArHelper.WithSession(s => { s.CreateSQLQuery("delete from Internet.WriteOff").ExecuteUpdate(); });
			}
			while (balance > 0) {
				billing.OnMethod();
				billing.Compute();
				SystemTime.Now = () => DateTime.Now.AddDays(countDays);
				countDays++;
				using (new SessionScope()) {
					client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
					balance = client.PhysicalClient.Balance;
				}
			}
			using (new SessionScope()) {
				var writeOffs = WriteOff.Queryable.Where(w => w.Client == client).ToList();
				var sum = writeOffs.Sum(w => w.WriteOffSum);
				var lastWriteoffAt = writeOffs.Max(w => w.WriteOffDate);
				var firstWriteoffAt = writeOffs.Min(w => w.WriteOffDate);
				var expectedSum = ((lastWriteoffAt - firstWriteoffAt).TotalDays + 1) * 3;

				Assert.That(Math.Round(Convert.ToDecimal(expectedSum), 2),
					Is.EqualTo(Math.Round(sum, 2)));
				Assert.That(firstWriteoffAt.Date, Is.EqualTo(DateTime.Now.AddDays(29).Date));
				writeOffs.Select(w => w.WriteOffSum).Each(c => Assert.That(c, Is.EqualTo(3m)));
			}
		}


		[Test]
		public void ActiveDeactive()
		{
			ClientService service;
			PhysicalClient physClient;
			const int countDays = 5;
			using (new SessionScope()) {
				physClient = _client.PhysicalClient;
				physClient.Balance = -10m;
				physClient.Update();
				_client.Disabled = true;
				_client.AutoUnblocked = true;
				_client.RatedPeriodDate = SystemTime.Now();
				_client.Update();

				service = Activate(typeof(DebtWork), SystemTime.Now().AddDays(countDays));
			}
			billing.OnMethod();
			billing.Compute();
			using (new SessionScope()) {
				_client.Refresh();
				Assert.IsFalse(_client.Disabled);
				service.CompulsoryDeactivate();
				Assert.IsTrue(_client.Disabled);
				billing.OnMethod();
				Assert.IsTrue(_client.Disabled);
				physClient = ActiveRecordMediator<PhysicalClient>.FindByPrimaryKey(physClient.Id);
				physClient.Balance = _client.GetPriceForTariff();
				physClient.Update();
			}
			billing.OnMethod();
			billing.OnMethod();
			using (new SessionScope()) {
				_client.Refresh();
				Assert.IsFalse(_client.Disabled);
				Assert.IsFalse(_client.PaidDay);
				Assert.IsTrue(_client.AutoUnblocked);
				Assert.IsNull(_client.RatedPeriodDate);

				service = new ClientService {
					Client = _client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = SystemTime.Now().AddDays(countDays),
					Service = Service.GetByType(typeof(VoluntaryBlockin)),
				};
				_client.ClientServices.Add(service);
				service.Activate();
				Assert.IsTrue(_client.Disabled);
			}
			billing.Compute();
			using (new SessionScope()) {
				service = ActiveRecordMediator<ClientService>.FindByPrimaryKey(service.Id);
				Assert.That(WriteOff.FindAll().Last().WriteOffSum, Is.GreaterThan(3m));
				service.CompulsoryDeactivate();
			}
			billing.OnMethod();
			billing.Compute();
			using (new SessionScope()) {
				_client = Client.Find(_client.Id);
				var userWriteOffs = UserWriteOff.Queryable.ToList();
				Assert.That(userWriteOffs.Count, Is.EqualTo(4), userWriteOffs.Implode());
				Assert.That(userWriteOffs[0].Sum, Is.EqualTo(50m));
				Assert.That(userWriteOffs[1].Sum, Is.GreaterThan(5m));
				Assert.That(userWriteOffs[1].Sum, Is.LessThan(25m));
				Assert.That(userWriteOffs[2].Sum, Is.EqualTo(50m));
				Assert.That(userWriteOffs[3].Sum, Is.GreaterThan(5m));
				Assert.That(userWriteOffs[3].Sum, Is.LessThan(25m));
				Assert.IsFalse(_client.Disabled);
			}
			billing.OnMethod();
			using (new SessionScope())
				_client.Refresh();
			Assert.IsFalse(_client.Disabled);
		}

		[Test]
		public void CanBlockTest()
		{
			using (new SessionScope()) {
				var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, -1000);
				client.Disabled = false;
				client.Save();
				Assert.IsTrue(client.CanBlock());
				var service = new ClientService {
					Client = client,
					Service = ActiveRecordMediator<DebtWork>.FindFirst()
				};
				ActiveRecordMediator.Save(service);

				client.Refresh();
				Assert.IsFalse(client.CanBlock());
				client.ClientServices.Remove(service);

				service = new ClientService {
					Client = client,
					Service = Service.GetByType(typeof(DebtWork)),
					BeginWorkDate = DateTime.Now,
					EndWorkDate = DateTime.Now.AddDays(1)
				};
				client.ClientServices.Add(service);

				SystemTime.Now = () => DateTime.Now;

				Assert.IsFalse(client.CanBlock());
				SystemTime.Now = () => DateTime.Now.AddDays(2);
				Assert.IsTrue(client.CanBlock());
				ActiveRecordMediator.Save(new ClientService {
					Client = client,
					Service = Service.GetByType(typeof(DebtWork))
				});

				client.Refresh();
				Assert.IsFalse(client.CanBlock());

				client.ClientServices.Clear();

				Assert.IsTrue(client.CanBlock());
				client.Disabled = true;
				client.Update();
				Assert.IsFalse(client.CanBlock());
			}
		}

		[Test]
		public void PostponedPayment()
		{
			Client client_Post;
			Client client_simple;
			PhysicalClient pclient_post;
			using (new SessionScope()) {
				client_Post = BaseBillingFixture.CreateAndSaveClient("testRated", false, 100);
				client_Post.RatedPeriodDate = DateTime.Now;
				client_Post.Save();
				client_simple = BaseBillingFixture.CreateAndSaveClient("testRated", false, 100);
				client_simple.Save();
				pclient_post = client_Post.PhysicalClient;
				pclient_post.Balance -= 200;
				pclient_post.Update();
			}
			billing.Compute();
			using (new SessionScope()) {
				client_simple.Refresh();
				client_Post.Refresh();
				Assert.IsTrue(client_Post.Disabled);
				Assert.IsFalse(client_simple.Disabled);

				new Payment {
					Client = client_Post,
					Sum = 1000,
					BillingAccount = false
				}.Save();
			}
			billing.OnMethod();
			using (new SessionScope()) {
				pclient_post = ActiveRecordMediator<PhysicalClient>.FindByPrimaryKey(pclient_post.Id);
				client_Post = ActiveRecordMediator<Client>.FindByPrimaryKey(client_Post.Id);
				client_Post.Disabled = true;
				ActiveRecordMediator.Save(client_Post);
				pclient_post.Balance = -200m;
				pclient_post.Update();

				var service = new ClientService {
					Client = client_Post,
					Service = Service.GetByType(typeof(DebtWork)),
					BeginWorkDate = DateTime.Now,
					EndWorkDate = DateTime.Now.AddDays(1)
				};
				client_Post.ClientServices.Add(service);
				service.Activate();
			}
			billing.OnMethod();
			billing.Compute();

			using (new SessionScope()) {
				client_simple.Refresh();
				client_Post.Refresh();
				Assert.IsFalse(client_Post.Disabled);
				Assert.IsFalse(client_simple.Disabled);
				SystemTime.Now = () => DateTime.Now.AddHours(25);
			}
			billing.OnMethod();
			billing.Compute();
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.Where(w => w.Client == client_Post).Count(), Is.EqualTo(2));

				client_simple.Refresh();
				client_Post = ActiveRecordMediator<Client>.FindByPrimaryKey(client_Post.Id);
				Assert.IsTrue(client_Post.Disabled);
				Assert.IsFalse(client_simple.Disabled);
				client_Post.Disabled = false;
				client_Post.Update();
			}
			billing.Compute();
			using (new SessionScope()) {
				client_simple.Refresh();
				client_Post.Refresh();
				Assert.IsTrue(client_Post.Disabled);
				Assert.IsFalse(client_simple.Disabled);

				client_Post.Refresh();
			}
		}

		[Test]
		public void WorkLawyerTest()
		{
			Client client;
			using (new SessionScope()) {
				client = new Client() {
					LawyerPerson = new LawyerPerson {
						Name = "testLawyerPerson",
						Balance = -3000,
						Tariff = 1000,
						Region = ArHelper.WithSession(s => s.Query<RegionHouse>().FirstOrDefault())
					}
				};
				client.Save();
			}
			billing.OnMethod();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsTrue(client.ShowBalanceWarningPage);
			}
			ClientService cService;
			using (new SessionScope()) {
				client.Refresh();
				cService = new ClientService {
					Client = client,
					EndWorkDate = DateTime.Now.AddDays(2),
					Service = Service.Type<WorkLawyer>()
				};
				ActiveRecordMediator.Save(cService);
			}
			billing.OnMethod();
			using (new SessionScope()) {
				client.Refresh();
				Assert.False(client.ShowBalanceWarningPage);
				Assert.That(client.ClientServices.Count, Is.EqualTo(1));
			}
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.Compute();
			using (new SessionScope()) {
				client.Refresh();
				Assert.False(client.ShowBalanceWarningPage);
				Assert.That(client.ClientServices.Count, Is.EqualTo(1));
			}
			SystemTime.Now = () => DateTime.Now.AddDays(2);
			billing.Compute();
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.True(client.ShowBalanceWarningPage);
				Assert.True(client.Disabled);
				Assert.That(client.ClientServices.Count, Is.EqualTo(0));
			}
		}

		[Test]
		public void Debt_work_and_payment()
		{
			using (new SessionScope()) {
				_client.PhysicalClient.Balance = 1;
				_client.Save();
			}
			billing.Compute();
			using (new SessionScope()) {
				_client.Refresh();
				Assert.That(_client.Disabled, Is.True);
				Activate(typeof(DebtWork));
				new Payment(_client, 550).Save();
			}
			billing.On();
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.On();
			using (new SessionScope()) {
				var client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				Assert.That(client.Disabled, Is.False);
				Assert.That(client.Status.Id, Is.EqualTo((uint)StatusType.Worked));
			}
		}

		private void AssertThatContainsOnlyMandatoryServices()
		{
			Assert.That(_client.ClientServices.Count, Is.EqualTo(2), _client.ClientServices.Implode(c => c.Service));
		}

		[Test]
		public void DebtWorkActivateDiactivate()
		{
			using (new SessionScope()) {
				_client.Disabled = true;
				_client.PhysicalClient.Balance = -5m;
				_client.Save();
				var service = Activate(typeof(DebtWork));
				Assert.IsFalse(_client.Disabled);
				SystemTime.Now = () => DateTime.Now.AddDays(2);
				service.Deactivate();
				Assert.IsTrue(_client.Disabled);
				service.Activate();
				Assert.IsTrue(_client.Disabled);
			}
		}

		[Test]
		public void Debt_work_payment()
		{
			using (new SessionScope()) {
				_client.Disabled = true;
				_client.PhysicalClient.Balance = -5m;
				_client.Save();
				Activate(typeof(DebtWork));
				ActiveRecordMediator.Save(new Payment(_client, _client.GetPriceForTariff() + 50));
				Assert.IsTrue(_client.ClientServices.Select(c => c.Service).Contains(Service.Type<DebtWork>()));
			}
			billing.OnMethod();
			using (new SessionScope()) {
				var client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				Assert.IsFalse(client.ClientServices.Select(c => c.Service).Contains(Service.Type<DebtWork>()));
			}
		}

		[Test]
		public void Debt_Work_diactivate_and_delete()
		{
			using (new SessionScope()) {
				_client.Disabled = true;
				_client.PhysicalClient.Balance = -5m;
				_client.Save();
				Activate(typeof(DebtWork));
				Assert.IsTrue(_client.ClientServices.Select(c => c.Service).Contains(Service.Type<DebtWork>()));
			}
			SystemTime.Now = () => DateTime.Now.AddDays(2);
			billing.OnMethod();
			using (new SessionScope()) {
				var client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				var service = client.ClientServices.FirstOrDefault(s => s.Service.Id == Service.Type<DebtWork>().Id);
				Assert.IsNotNull(service);
				Assert.IsTrue(service.Diactivated);
				Assert.False(service.Activated);
				ActiveRecordMediator.Save(new Payment(_client, _client.GetPriceForTariff() + 50));
			}
			billing.OnMethod();
			using (new SessionScope()) {
				var client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				var service = client.ClientServices.FirstOrDefault(s => s.Service.Id == Service.Type<DebtWork>().Id);
				Assert.IsNull(service);
			}
		}

		[Test]
		public void New_client_debt_work()
		{
			using (new SessionScope()) {
				_client = CreateClient();
				_client.Disabled = true;
				_client.BeginWork = null;
				_client.PhysicalClient.Balance = 0;
				ActiveRecordMediator.Save(_client);
				Activate(typeof(DebtWork), DateTime.Now.AddDays(3));
			}
			billing.OnMethod();
			using (new SessionScope()) {
				var client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				Assert.IsFalse(client.Disabled);
			}
		}

		[Test(Description = "проверяю, что не выдается инсключение при данных условиях")]
		public void Tv_box_and_no_rated_date()
		{
			using (new SessionScope()) {
				Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(100));
				var clientService = new ClientService(_client, Service.GetByType(typeof(IpTvBoxRent)));
				_client.Activate(clientService);
				_client.PaidDay = false;
				_client.RatedPeriodDate = null;
				ActiveRecordMediator.Save(_client);
			}
			billing.OnMethod();
			billing.Compute();
			using (new SessionScope()) {
				var settings = ActiveRecordMediator<InternetSettings>.FindFirst();
				Assert.IsFalse(settings.LastStartFail);
			}
		}
	}
}