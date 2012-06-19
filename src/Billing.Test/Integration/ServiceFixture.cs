using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Services;
using NHibernate.Impl;
using NUnit.Framework;
using Test.Support.log4net;

namespace Billing.Test.Integration
{
	public class ServiceFixture : MainBillingFixture
	{
		private ClientService Activate(Type type, DateTime? endDate = null)
		{
			if (endDate == null)
				endDate = DateTime.Now.AddDays(1);

			var service = new ClientService {
				Client = _client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = endDate.Value,
				Service = Service.GetByType(type),
				Activator = InitializeContent.Partner
			};

			_client.Activate(service);

			return service;
		}

		[Test]
		public void VolBlockIfMinimumBalance()
		{
			using (new SessionScope()) {
				_client.FreeBlockDays = 28;
				_client.PhysicalClient.Balance = _client.GetPrice()/_client.GetInterval() - 5;
				_client.Save();
				Assert.That(_client.PhysicalClient.Balance, Is.GreaterThan(0));

				Activate(typeof (VoluntaryBlockin));

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
			Activate(typeof(VoluntaryBlockin));
			new Payment(_client, 500).Save();

			billing.OnMethod();

			_client.Refresh();
			Assert.That(_client.Disabled, Is.True);
		}

		//Если не поперло, запусти 3 раза отдельно
		[Test]
		public void VoluntaryBlockingTest2()
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
				WriteOff.DeleteAll();

				service = Activate(typeof (VoluntaryBlockin), DateTime.Now.AddDays(100));
				Assert.That(_client.FreeBlockDays, Is.EqualTo(MainBilling.FreeDaysVoluntaryBlockin));
				service.CompulsoryDeactivate();
				Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(1));
			}
			billing.Compute();
			using (new SessionScope()) {
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(0));
				Assert.That(_client.FreeBlockDays, Is.EqualTo(MainBilling.FreeDaysVoluntaryBlockin));
				service = new ClientService {
					Client = _client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = DateTime.Now.AddDays(100),
					Service = Service.GetByType(typeof (VoluntaryBlockin))
				};
				_client.ClientServices.Add(service);
				service.Activate();
			}

			Thread.Sleep(500);
			for (int i = 0; i < MainBilling.FreeDaysVoluntaryBlockin + 1; i++) {
				SystemTime.Now = () => DateTime.Now.AddDays(i);
				billing.Compute();
			}

			using (new SessionScope()) {
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				service = ActiveRecordMediator<ClientService>.FindByPrimaryKey(service.Id);
				Assert.That(_client.FreeBlockDays, Is.EqualTo(0));

				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(0));

				service.CompulsoryDeactivate();
				service = new ClientService {
					Client = _client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = DateTime.Now.AddDays(100),
					Service = Service.GetByType(typeof (VoluntaryBlockin))
				};
				_client.ClientServices.Add(service);
				service.Activate();
			}
			billing.Compute();
			using (new SessionScope())
				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(0));
			billing.Compute();
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(1));
				Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(4));

				service.CompulsoryDeactivate();
			}
			billing.Compute();
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(1));
				_client = ActiveRecordMediator<Client>.FindByPrimaryKey(_client.Id);
				_client.ClientServices.Clear();

				SystemTime.Reset();
				UserWriteOff.DeleteAll();
				WriteOff.DeleteAll();

				Activate(typeof (VoluntaryBlockin), DateTime.Now.AddDays(4));
				Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(2));
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
			const int countDays = 5;
			var client = _client;

			PhysicalClient physClient;
			ClientService CServive;
			
			using (new SessionScope()) {
				physClient = client.PhysicalClient;
				physClient.Balance = -10m;
				physClient.Update();
				client.Disabled = true;
				client.AutoUnblocked = true;
				client.RatedPeriodDate = SystemTime.Now();
				client.Update();

				CServive = new ClientService {
				    Client = client,
				    BeginWorkDate = DateTime.Now,
				    EndWorkDate = SystemTime.Now().AddDays(countDays),
				    Service = Service.GetByType(typeof (DebtWork)),
				};

				client.ClientServices.Add(CServive);
				CServive.Activate();
			}
			for (int i = 0; i < countDays; i++) {
				billing.OnMethod();
				billing.Compute();
				SystemTime.Now = () => DateTime.Now.AddDays(i + 1);
			}
			using (new SessionScope()) {
				client.Refresh();
				Assert.That(physClient.Balance, Is.EqualTo(-10));
				new Payment {
					Client = client,
					Sum = client.PhysicalClient.Tariff.Price,
					PaidOn = DateTime.Now.AddDays(-1)
				}.Save();
			}
			billing.OnMethod();
			using (new SessionScope()) {
				physClient = ActiveRecordMediator<PhysicalClient>.FindByPrimaryKey(physClient.Id);
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				physClient.Balance = -10;
				client.RatedPeriodDate = DateTime.Now;
				client.Update();
				physClient.Update();
				client.Refresh();
				SystemTime.Reset();
				CServive = ActiveRecordMediator<ClientService>.FindByPrimaryKey(CServive.Id);
				CServive.Activate();
			}
			for (int i = 0; i < countDays; i++) {
				billing.OnMethod();
				billing.Compute();
				int i1 = i;
				SystemTime.Now = () => DateTime.Now.AddDays(i1 + 1);
			}
			SystemTime.Now = () => DateTime.Now.AddDays(countDays + 1);
			billing.OnMethod();
			using (new SessionScope()) {
				physClient = ActiveRecordMediator<PhysicalClient>.FindByPrimaryKey(physClient.Id);
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.That(WriteOff.FindAll().Count(), Is.EqualTo(countDays));
				Assert.That(physClient.Balance, Is.LessThan(0m));
				Assert.IsTrue(client.Disabled);
				client.Disabled = false;
				client.Update();

				Assert.That(Math.Round(-client.GetPrice()/client.GetInterval()*countDays, 0) - 10,
							Is.EqualTo(Math.Round(physClient.Balance, 0)));
				Assert.That(client.RatedPeriodDate.Value.Date, Is.EqualTo(DateTime.Now.Date));
				CServive = new ClientService {
					Client = client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = SystemTime.Now().AddDays(countDays),
					Service = Service.GetByType(typeof (DebtWork)),
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
				physClient = ActiveRecordMediator<PhysicalClient>.FindByPrimaryKey(physClient.Id);
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				physClient.Balance = -10;
				physClient.Update();

				CServive = new ClientService {
					Client = client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = SystemTime.Now().AddDays(countDays),
					Service = Service.GetByType(typeof (DebtWork)),
				};
				client.ClientServices.Add(CServive);

				CServive.Activate();
				Assert.That(CServive.Activated, Is.EqualTo(true));
			}
		}

		[Test]
		public void DebtWorkTestPartner()
		{
			using (new SessionScope()) {
				PrepareTest();
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
					Service = Service.GetByType(typeof (DebtWork)),
					Activator = InitializeContent.Partner
				};
				client.ClientServices.Add(cServive);

				cServive.Activate();
				Assert.That(cServive.Activated, Is.EqualTo(true));
				Assert.IsFalse(cServive.Client.Disabled);
				cServive.CompulsoryDeactivate();
				Assert.IsTrue(cServive.Client.Disabled);

				Assert.That(client.ClientServices, Is.Empty);
			}
		}

		[Test]
		public void VoluntaryBlockinTest()
		{
			int countDays = 10;
			Client client;
			ClientService service;
			using (new SessionScope()) {

				PrepareTest();
				client = CreateClient();

				client.AutoUnblocked = true;
				client.FreeBlockDays = MainBilling.FreeDaysVoluntaryBlockin;
				client.Update();

				service = new ClientService {
					Client = client,
					Activator = InitializeContent.Partner,
					Service = Service.GetByType(typeof (VoluntaryBlockin)),
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
				WriteOff.DeleteAll();
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
				Assert.That(client.ClientServices, Is.Empty);
			}
			SystemTime.Now = () => service.EndWorkDate.Value.AddDays(46);
			billing.OnMethod();

			using (new SessionScope()) {
				client.Refresh();
				Assert.That(client.ClientServices, Is.Empty);
				Assert.IsFalse(client.Disabled);
			}

			SystemTime.Reset();
			var balance = 0m;
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				service = new ClientService {
					Client = client,
					Activator = InitializeContent.Partner,
					Service = Service.GetByType(typeof (VoluntaryBlockin)),
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
				WriteOff.DeleteAll();
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
				var firstdate = WriteOff.FindFirst().WriteOffDate;
				Assert.That(
					Math.Round(
						Convert.ToDecimal(
							((WriteOff.FindAll().Last().WriteOffDate - WriteOff.FindFirst().WriteOffDate).TotalDays + 1)*3),
						2),
					Is.EqualTo(Math.Round(
						WriteOff.Queryable.Where(w => w.Client == client).ToList().Sum(w => w.WriteOffSum), 2)));
				Assert.That(firstdate.Date, Is.EqualTo(DateTime.Now.AddDays(29).Date));
				WriteOff.FindAll().Select(w => w.WriteOffSum).Each(c => Assert.That(c, Is.EqualTo(3m)));
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
					Service = Service.GetByType(typeof (VoluntaryBlockin)),
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
				Console.WriteLine(userWriteOffs.Implode());
				Assert.That(userWriteOffs.Count, Is.EqualTo(3), userWriteOffs.Implode());
				Assert.That(userWriteOffs[0].Sum, Is.GreaterThan(5m));
				Assert.That(userWriteOffs[0].Sum, Is.LessThan(25m));
				Assert.That(userWriteOffs[1].Sum, Is.EqualTo(50m));
				Assert.That(userWriteOffs[2].Sum, Is.GreaterThan(5m));
				Assert.That(userWriteOffs[2].Sum, Is.LessThan(25m));
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
				Service = Service.GetByType(typeof (DebtWork)),
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
				Service = Service.GetByType(typeof (DebtWork))
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
				pclient_post.Balance = -200m;
				pclient_post.Update();

				var service = new ClientService {
					Client = client_Post,
					Service = Service.GetByType(typeof (DebtWork)),
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
						Balance = -2000,
						Tariff = 1000
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
	}
}
