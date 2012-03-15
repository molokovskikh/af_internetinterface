using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	public class ServiceFixture : MainBillingFixture
	{
		[Test]
		public void VolBlockIfMinimumBalance()
		{
			var client = CreateClient();
			client.FreeBlockDays = 28;
			client.PhysicalClient.Balance = client.GetPrice()/client.GetInterval() - 5;
			client.Save();
			Assert.That(client.PhysicalClient.Balance, Is.GreaterThan(0));

			var cService = new ClientService {
				Client = client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = DateTime.Now.AddDays(1),
				Service = Service.GetByType(typeof (VoluntaryBlockin))
			};

			client.ClientServices.Add(cService);
			cService.Activate();

			Assert.IsTrue(client.PaidDay);

			billing.Compute();

			SystemTime.Now = () => DateTime.Now.AddDays(1);

			billing.Compute();

			Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(1));
			Assert.That(WriteOff.Queryable.Where(w => w.Client == client).Count(), Is.EqualTo(0));
			Assert.IsFalse(client.PaidDay);
		}

		//Если не поперло, запусти 3 раза отдельно
		[Test]
		public void VoluntaryBlockingTest2()
		{
			_client.YearCycleDate = DateTime.Now.AddYears(-1);
			billing.Compute();
			Assert.That(_client.FreeBlockDays, Is.EqualTo(MainBilling.FreeDaysVoluntaryBlockin));
			WriteOff.DeleteAll();
			var cService = new ClientService {
				Client = _client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = DateTime.Now.AddDays(100),
				Service = Service.GetByType(typeof (VoluntaryBlockin))
			};

			_client.ClientServices.Add(cService);
			cService.Activate();
			Assert.That(_client.FreeBlockDays, Is.EqualTo(MainBilling.FreeDaysVoluntaryBlockin));
			cService.CompulsoryDiactivate();
			Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(1));
			billing.Compute();
			Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(0));
			Assert.That(_client.FreeBlockDays, Is.EqualTo(MainBilling.FreeDaysVoluntaryBlockin));
			cService = new ClientService {
				Client = _client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = DateTime.Now.AddDays(100),
				Service = Service.GetByType(typeof (VoluntaryBlockin))
			};
			_client.ClientServices.Add(cService);
			cService.Activate();

			Thread.Sleep(500);
			for (int i = 0; i < MainBilling.FreeDaysVoluntaryBlockin + 1; i++) {
				SystemTime.Now = () => DateTime.Now.AddDays(i);
				billing.Compute();
			}
			var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			var session = sessionHolder.CreateSession(typeof (ActiveRecordBase));
			session.Flush();

			_client.Refresh();

			Assert.That(_client.FreeBlockDays, Is.EqualTo(0));

			Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(0));

			cService.CompulsoryDiactivate();
			cService = new ClientService {
				Client = _client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = DateTime.Now.AddDays(100),
				Service = Service.GetByType(typeof (VoluntaryBlockin))
			};
			_client.ClientServices.Add(cService);
			cService.Activate();

			billing.Compute();
			Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(0));
			billing.Compute();
			Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(1));

			Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(4));

			cService.CompulsoryDiactivate();

			billing.Compute();
			Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(1));

			cService = new ClientService {
				Client = _client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = DateTime.Now.AddDays(4),
				Service = Service.GetByType(typeof (VoluntaryBlockin))
			};
			SystemTime.Reset();
			UserWriteOff.DeleteAll();
			WriteOff.DeleteAll();

			_client.ClientServices.Add(cService);
			cService.Activate();

			Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(2));

			for (int i = 0; i < 5; i++) {
				SystemTime.Now = () => DateTime.Now.AddDays(i);
				billing.OnMethod();
				billing.Compute();
			}

			Assert.That(WriteOff.Queryable.Count(), Is.EqualTo(3));

			Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(3));
		}

		[Test]
		public void DebtWorkTest()
		{
			PrepareTest();
			SystemTime.Reset();
			var client = CreateClient();
			const int countDays = 5;
			var physClient = client.PhysicalClient;
			physClient.Balance = -10m;
			physClient.Update();
			client.Disabled = true;
			client.AutoUnblocked = true;
			client.RatedPeriodDate = SystemTime.Now();
			client.Update();

			var CServive = new ClientService {
				Client = client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = SystemTime.Now().AddDays(countDays),
				Service = Service.GetByType(typeof (DebtWork)),
			};

			client.ClientServices.Add(CServive);
			CServive.Activate();
			for (int i = 0; i < countDays; i++) {
				billing.OnMethod();
				billing.Compute();
				SystemTime.Now = () => DateTime.Now.AddDays(i + 1);
			}
			Assert.That(physClient.Balance, Is.EqualTo(-10));
			new Payment {
				Client = client,
				Sum = client.PhysicalClient.Tariff.Price,
				PaidOn = DateTime.Now.AddDays(-1)
			}.Save();
			billing.OnMethod();
			physClient.Balance = -10;
			client.RatedPeriodDate = DateTime.Now;
			client.Update();
			physClient.Update();
			client.Refresh();
			SystemTime.Reset();

			CServive.Activate();
			for (int i = 0; i < countDays; i++) {
				billing.OnMethod();
				billing.Compute();
				int i1 = i;
				SystemTime.Now = () => DateTime.Now.AddDays(i1 + 1);
			}
			SystemTime.Now = () => DateTime.Now.AddDays(countDays + 1);
			billing.OnMethod();

			Assert.That(WriteOff.FindAll().Count(), Is.EqualTo(countDays));
			Assert.That(physClient.Balance, Is.LessThan(0m));
			Assert.IsTrue(client.Disabled);
			client.Disabled = false;
			client.Update();

			Assert.That(Math.Round(-client.GetPrice()/client.GetInterval()*countDays, 5) - 10,
			            Is.EqualTo(Math.Round(physClient.Balance, 5)));
			Assert.That(client.RatedPeriodDate.Value.Date, Is.EqualTo(DateTime.Now.Date));
			CServive = new ClientService {
				Client = client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = SystemTime.Now().AddDays(countDays),
				Service = Service.GetByType(typeof (DebtWork)),
			};
			client.ClientServices.Add(CServive);

			Assert.That(CServive.LogComment, !Is.EqualTo(string.Empty));
			client.Disabled = true;
			client.Update();
			new Payment {
				Client = client,
				Sum = client.PhysicalClient.Tariff.Price,
				PaidOn = DateTime.Now.AddDays(1)
			}.Save();
			SystemTime.Now = () => DateTime.Now.AddDays(countDays + 1);
			billing.OnMethod();
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

		[Test]
		public void DebtWorkTestPartner()
		{
			PrepareTest();
			SystemTime.Reset();
			var client = CreateClient();
			const int countDays = 10;
			var physClient = client.PhysicalClient;
			physClient.Balance = -10m;
			physClient.Update();
			client.Disabled = true;
			client.AutoUnblocked = true;
			client.RatedPeriodDate = SystemTime.Now();
			client.Update();

			var CServive = new ClientService {
				Client = client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = SystemTime.Now().AddDays(countDays),
				Service = Service.GetByType(typeof (DebtWork)),
				Activator = InitializeContent.Partner
			};
			client.ClientServices.Add(CServive);

			CServive.Refresh();
			CServive.Activate();
			Assert.That(CServive.Activated, Is.EqualTo(true));
			Assert.IsFalse(CServive.Client.Disabled);
			CServive.CompulsoryDiactivate();
			Assert.IsTrue(CServive.Client.Disabled);

			Assert.That(client.ClientServices, Is.Empty);
		}

		[Test]
		public void VoluntaryBlockinTest()
		{
			SystemTime.Reset();
			Client client;
			using (var tr = new TransactionScope(OnDispose.Rollback)) {
				PrepareTest();
				client = CreateClient();
				tr.VoteCommit();
			}
			int countDays = 10;
			var physClient = client.PhysicalClient;

			client.AutoUnblocked = true;
			client.FreeBlockDays = MainBilling.FreeDaysVoluntaryBlockin;
			client.Update();

			var service = new ClientService {
				Client = client,
				Activator = InitializeContent.Partner,
				Service = Service.GetByType(typeof (VoluntaryBlockin)),
				BeginWorkDate = DateTime.Now.AddDays(2),
				EndWorkDate = DateTime.Now.AddDays(countDays + 2)
			};

			client.ClientServices.Add(service);
			service.Activate();
			billing.OnMethod();
			billing.Compute();
			client.Refresh();
			Assert.IsFalse(client.Disabled);
			WriteOff.DeleteAll();
			SystemTime.Now = () => DateTime.Now.AddDays(2);
			billing.OnMethod();
			client.Refresh();
			Assert.IsTrue(client.Disabled);
			for (int i = 0; i < countDays; i++) {
				billing.OnMethod();
				billing.Compute();
				int i1 = i;
				SystemTime.Now = () => DateTime.Now.AddDays(i1 + 3);
			}
			Assert.That(WriteOff.Queryable.Where(c => c.Client == client).ToList().Sum(w => w.WriteOffSum),
			            Is.EqualTo(0));
			SystemTime.Now = () => DateTime.Now.AddDays(countDays + 3);
			billing.OnMethod();

			Assert.IsFalse(client.Disabled);
			Assert.That(client.ClientServices, Is.Empty);
			SystemTime.Now = () => service.EndWorkDate.Value.AddDays(46);
			billing.OnMethod();
			Assert.That(client.ClientServices, Is.Empty);
			Assert.IsFalse(client.Disabled);
			SystemTime.Reset();
			using (var tr = new TransactionScope(OnDispose.Rollback)) {
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
				tr.VoteCommit();
			}
			countDays = 0;

			client.FreeBlockDays = MainBilling.FreeDaysVoluntaryBlockin;
			client.Update();
			while (physClient.Balance > 0) {
				billing.OnMethod();
				using (var tr = new TransactionScope(OnDispose.Rollback)) {
					billing.Compute();
					tr.VoteCommit();
				}
				SystemTime.Now = () => DateTime.Now.AddDays(countDays);
				countDays++;
				physClient = PhysicalClients.FindFirst();
			}
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


		[Test]
		public void ActiveDeactive()
		{
			PrepareTest();
			SystemTime.Reset();
			var client = CreateClient();
			const int countDays = 5;
			var physClient = client.PhysicalClient;
			physClient.Balance = -10m;
			physClient.Update();
			client.Disabled = true;
			client.AutoUnblocked = true;
			client.RatedPeriodDate = SystemTime.Now();
			client.Update();

			var CServive = new ClientService {
				Client = client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = SystemTime.Now().AddDays(countDays),
				Service = Service.GetByType(typeof (DebtWork)),
				Activator = InitializeContent.Partner
			};
			client.ClientServices.Add(CServive);
			CServive.Activate();
			billing.OnMethod();
			billing.Compute();
			Assert.IsFalse(client.Disabled);
			CServive.CompulsoryDiactivate();
			Assert.IsTrue(client.Disabled);
			billing.OnMethod();
			Assert.IsTrue(client.Disabled);
			CServive = new ClientService {
				Client = client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = SystemTime.Now().AddDays(countDays),
				Service = Service.GetByType(typeof (VoluntaryBlockin)),
				Activator = InitializeContent.Partner
			};
			physClient.Balance = client.GetPriceForTariff();
			physClient.Update();
			billing.OnMethod();
			Assert.IsFalse(client.Disabled);
			Assert.IsFalse(client.PaidDay);
			Assert.IsTrue(client.AutoUnblocked);
			Assert.IsNull(client.RatedPeriodDate);

			client.ClientServices.Add(CServive);
			CServive.Activate();
			Assert.IsTrue(client.Disabled);

			billing.Compute();
			Assert.That(WriteOff.FindAll().Last().WriteOffSum, Is.GreaterThan(3m));
			CServive.CompulsoryDiactivate();
			billing.OnMethod();
			billing.Compute();

			var userWriteOffs = UserWriteOff.Queryable.ToList();

			Assert.That(userWriteOffs.Count, Is.EqualTo(3));
			Assert.That(userWriteOffs[0].Sum, Is.GreaterThan(5m));
			Assert.That(userWriteOffs[0].Sum, Is.LessThan(25m));
			Assert.That(userWriteOffs[1].Sum, Is.EqualTo(50m));
			Assert.That(userWriteOffs[2].Sum, Is.GreaterThan(5m));
			Assert.That(userWriteOffs[2].Sum, Is.LessThan(25m));
			Assert.IsFalse(client.Disabled);
			billing.OnMethod();
			Assert.IsFalse(client.Disabled);
		}

		[Test]
		public void CanBlockTest()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, -1000);
			client.Disabled = false;
			client.Save();
			Assert.IsTrue(client.CanBlock());
			var service = new ClientService {
				Client = client,
				Service = ActiveRecordMediator<DebtWork>.FindFirst()
			};
			service.Save();

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
			client.ClientServices.Add(service);
			new ClientService {
				Client = client,
				Service = Service.GetByType(typeof (DebtWork))
			}.Save();

			client.Refresh();
			Assert.IsFalse(client.CanBlock());

			client.ClientServices.Clear();

			Assert.IsTrue(client.CanBlock());
			client.Disabled = true;
			client.Update();
			Assert.IsFalse(client.CanBlock());
		}

		[Test]
		public void PostponedPayment()
		{
			var client_Post = BaseBillingFixture.CreateAndSaveClient("testRated", false, 100);
			client_Post.RatedPeriodDate = DateTime.Now;
			client_Post.Save();
			var client_simple = BaseBillingFixture.CreateAndSaveClient("testRated", false, 100);
			client_simple.Save();
			var pclient_post = client_Post.PhysicalClient;
			pclient_post.Balance -= 200;
			pclient_post.Update();

			billing.Compute();

			client_simple.Refresh();
			client_Post.Refresh();
			Assert.IsTrue(client_Post.Disabled);
			Assert.IsFalse(client_simple.Disabled);

			new Payment {
				Client = client_Post,
				Sum = 1000,
				BillingAccount = false
			}.Save();

			billing.OnMethod();

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

			billing.OnMethod();
			billing.Compute();


			client_simple.Refresh();
			client_Post.Refresh();
			Assert.IsFalse(client_Post.Disabled);
			Assert.IsFalse(client_simple.Disabled);

			SystemTime.Now = () => DateTime.Now.AddHours(25);

			billing.OnMethod();
			billing.Compute();

			Assert.That(WriteOff.Queryable.Where(w => w.Client == client_Post).Count(), Is.EqualTo(2));

			client_simple.Refresh();
			client_Post.Refresh();
			Assert.IsTrue(client_Post.Disabled);
			Assert.IsFalse(client_simple.Disabled);

			client_Post.Disabled = false;
			client_Post.Update();

			billing.Compute();

			client_simple.Refresh();
			client_Post.Refresh();
			Assert.IsTrue(client_Post.Disabled);
			Assert.IsFalse(client_simple.Disabled);

			client_Post.Refresh();
		}
	}
}
