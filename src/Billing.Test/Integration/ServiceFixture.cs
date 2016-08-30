using System;
using System.Configuration;
using System.Globalization;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Controllers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Models;
using InternetInterface.Models.Services;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Linq;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	public class ServiceFixture : MainBillingFixture
	{
		private ClientService Activate(Type type, DateTime? endDate = null)
		{
			client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);

			if (endDate == null)
				endDate = DateTime.Now.AddDays(1);

			var service = new ClientService {
				Client = client,
				BeginWorkDate = DateTime.Now,
				EndWorkDate = endDate.Value,
				Service = Service.GetByType(type)
			};

			client.Activate(service);

			ActiveRecordMediator.Save(client);

			return service;
		}

		[Test]
		public void VolBlockIfMinimumBalance()
		{
			using (new SessionScope()) {
				client.FreeBlockDays = 28;
				client.PhysicalClient.Balance = client.GetPrice() / client.GetInterval() - 5;
				client.Save();
				Assert.That(client.PhysicalClient.Balance, Is.GreaterThan(0));

				Activate(typeof(VoluntaryBlockin));

				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.IsTrue(client.PaidDay);
			}

			billing.ProcessWriteoffs();
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.ProcessWriteoffs();

			using (new SessionScope()) {
				client.Refresh();
				Assert.That(UserWriteOff.Queryable.AsQueryable().Count(), Is.EqualTo(1));
				Assert.That(WriteOff.Queryable.AsQueryable().Count(w => w.Client == client), Is.EqualTo(0));
				Assert.IsFalse(client.PaidDay);
			}
		}

		[Test]
		public void Do_not_deactivate_voluntary_blocking_on_payment()
		{
			using (new SessionScope()) {
				Activate(typeof(VoluntaryBlockin));
			}

			new Payment(client, 500).Save();

			billing.ProcessPayments();

			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.That(client.Disabled, Is.True);
			}
		}

		[Test]
		public void Year_circle_date()
		{
			using (new SessionScope()) {
				client.YearCycleDate = null;
				client.BeginWork = null;
				ActiveRecordMediator.Save(client);
			}
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.IsNull(client.YearCycleDate);
				client.BeginWork = DateTime.Now;
				ActiveRecordMediator.Save(client);
			}
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.IsNotNull(client.YearCycleDate);
			}
		}

		[Test]
		public void Free_days_test_vol_block()
		{
			ClientService service;
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				client.YearCycleDate = DateTime.Now.AddYears(-1);
			}
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.That(client.FreeBlockDays, Is.EqualTo(28));
				ArHelper.WithSession(s => { s.CreateSQLQuery("delete from Internet.WriteOff").ExecuteUpdate(); });
				Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(100));
			}
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				service = client.FindActiveService<VoluntaryBlockin>();
				Assert.That(client.FreeBlockDays, Is.EqualTo(28));
				service.ForceDeactivate();
				Assert.That(UserWriteOff.Queryable.Count(), Is.EqualTo(1));
			}
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				var writeOffs = ActiveRecordMediator.FindAll(typeof(WriteOff)).Cast<WriteOff>().ToList();
				Assert.That(writeOffs.Count, Is.EqualTo(0), writeOffs.Implode());
				Assert.That(client.FreeBlockDays, Is.EqualTo(28));
			}
		}

		[Test]
		public void Decrement_freedays_vol_block()
		{
			using (new SessionScope()) {
				client.FreeBlockDays = 28;
				ActiveRecordMediator.Save(client);
				Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(100));
			}

			for (int i = 0; i < 28 + 1; i++) {
				SystemTime.Now = () => DateTime.Now.AddDays(i);
				billing.ProcessWriteoffs();
			}

			using (new SessionScope()) {
				Assert.That(UserWriteOff.Queryable.AsQueryable().Count(), Is.EqualTo(1), UserWriteOff.Queryable.Implode());
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.That(client.FreeBlockDays, Is.EqualTo(0));
				Assert.That(WriteOff.Queryable.AsQueryable().Count(), Is.EqualTo(1), "\nНет разового списания за услугу");
			}
		}

		[Test]
		public void If_free_block_days_zero()
		{
			ClientService service;
			using (new SessionScope()) {
				service = Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(100));
			}
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.AsQueryable().Count(), Is.EqualTo(0));
			}
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.AsQueryable().Count(), Is.EqualTo(1));
				Assert.That(UserWriteOff.Queryable.AsQueryable().Count(), Is.EqualTo(2), UserWriteOff.Queryable.Implode());

				service = ActiveRecordMediator<ClientService>.FindByPrimaryKey(service.Id);
				service.ForceDeactivate();
			}
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.AsQueryable().Count(), Is.EqualTo(1));
			}
		}

		//Если не поперло, запусти 3 раза отдельно
		[Test]
		public void VoluntaryBlockingTest2()
		{
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				var forRemove = client.ClientServices.Where(s => NHibernateUtil.GetClass(s.Service) != typeof(Internet)).ToList();
				foreach (var assignedService in forRemove) {
					client.ClientServices.Remove(assignedService);
				}
				ActiveRecordMediator.Save(client);
			}
			using (new SessionScope()) {
				SystemTime.Reset();
				Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(4));
				Assert.That(ActiveRecordMediator<UserWriteOff>.FindAll().Length, Is.EqualTo(2));
			}
			for (int i = 0; i < 5; i++) {
				SystemTime.Now = () => DateTime.Now.AddDays(i);
				billing.ProcessPayments();
				billing.ProcessWriteoffs();
			}
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.AsQueryable().Count(), Is.EqualTo(3));

				var userWriteOffs = UserWriteOff.Queryable.ToList();
				Assert.That(userWriteOffs.Count(), Is.EqualTo(3), userWriteOffs.Implode());
			}
		}

		[Test]
		public void DebtWorkTest()
		{
			const int countDays = 3;
			var client = base.client;

			PhysicalClient physicalClient;
			ClientService CServive;

			using (new SessionScope()) {
				physicalClient = client.PhysicalClient;
				physicalClient.Balance = -10m;
				physicalClient.Update();
				client.Disabled = true;
				client.AutoUnblocked = true;
				client.RatedPeriodDate = SystemTime.Now();
				client.StartNoBlock = SystemTime.Now();
				client.Update();

				CServive = new ClientService {
					Client = client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = SystemTime.Now().AddDays(countDays),
					Service = Service.GetByType(typeof(DebtWork)),
				};

				client.ClientServices.Add(CServive);
				CServive.TryActivate();
			}
			for (var i = 0; i < countDays; i++) {
				billing.ProcessPayments();
				billing.ProcessWriteoffs();
				var i1 = i;
				SystemTime.Now = () => DateTime.Now.AddDays(i1 + 1);
			}
			SystemTime.Now = () => DateTime.Now.AddDays(countDays + 1);
			billing.ProcessPayments();
			using (new SessionScope()) {
				physicalClient = ActiveRecordMediator<PhysicalClient>.FindByPrimaryKey(physicalClient.Id);
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.That(WriteOff.FindAll().Count(), Is.EqualTo(countDays));
				Assert.That(physicalClient.Balance, Is.LessThan(0m));
				Assert.IsTrue(client.Disabled);
				Assert.IsNull(client.StartNoBlock);
				client.Disabled = false;
				client.Update();
				Assert.That(Math.Round(-client.GetPrice() / client.GetInterval() * countDays, 0) - 10,
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
			billing.ProcessPayments();
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

				CServive.TryActivate();
				Assert.That(CServive.IsActivated, Is.EqualTo(true));
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

				cServive.TryActivate();
				client.SaveAndFlush();
				Assert.That(cServive.IsActivated, Is.EqualTo(true));
				Assert.IsFalse(cServive.Client.Disabled);
				cServive.ForceDeactivate();
				Assert.IsTrue(cServive.Client.Disabled);
				//А вот и не должна, теперь мы их не удаляем
				//Assert.That(client.ClientServices.Count, Is.EqualTo(2), "должна остаться только услуга internet");
			}
		}

		[Test]
		public void Voluntary_blocking_and_minimum_balance()
		{
			decimal sum;
			using (new SessionScope()) {
				client = CreateClient();
				client.PhysicalClient.Balance = 5m;
				client.AutoUnblocked = true;
				client.FreeBlockDays = 0;
				client.Save();
				var daysInInterval = client.GetInterval();
				var price = client.GetPrice();
				sum = price / daysInInterval;
			}
			using (new SessionScope()) {
				Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(7));
			}
			for (int i = 0; i < 5; i++) {
				billing.ProcessPayments();
				billing.ProcessWriteoffs();
			}
			using (new SessionScope()) {
				client = Client.Find(client.Id);
				Assert.IsTrue(client.Disabled);
				AssertThatContainsOnlyMandatoryServices();
				Assert.That(client.PhysicalClient.Balance, Is.EqualTo(Math.Round(-50 - sum + 5, 2)), client.UserWriteOffs.Implode());
			}
		}

		[Test]
		public void Debt_work_and_payment_whith_payment()
		{
			using (new SessionScope()) {
				client = CreateClient();
				client.PhysicalClient.Balance = -5m;
				client.AutoUnblocked = true;
				client.Disabled = true;
				client.PercentBalance = 0.8m;
				client.Save();
			}
			using (new SessionScope()) {
				Activate(typeof(DebtWork));
			}
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client = Client.Find(client.Id);
				Assert.IsFalse(client.Disabled);
				new Payment(client, 100).Save();
			}
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client = Client.Find(client.Id);
				Assert.IsTrue(client.Disabled);
			}
			using (new SessionScope()) {
				client = CreateClient();
				client.PhysicalClient.Balance = -5m;
				client.AutoUnblocked = true;
				client.Disabled = true;
				client.PercentBalance = 0.8m;
				client.Save();
				Activate(typeof(DebtWork));
			}
			using (new SessionScope()) {
				client = Client.Find(client.Id);
				Assert.IsFalse(client.Disabled);
				new Payment(client, client.GetPriceForTariff()).Save();
			}
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client = Client.Find(client.Id);
				Assert.IsFalse(client.Disabled);
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
				client.FreeBlockDays = 28;
				client.Update();

				service = new ClientService {
					Client = client,
					Activator = InitializeContent.Partner,
					Service = Service.GetByType(typeof(VoluntaryBlockin)),
					BeginWorkDate = DateTime.Now.AddDays(2),
					EndWorkDate = DateTime.Now.AddDays(countDays + 2)
				};

				client.ClientServices.Add(service);
				service.TryActivate();
			}
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsFalse(client.Disabled);
				ArHelper.WithSession(s => { s.CreateSQLQuery("delete from Internet.WriteOff").ExecuteUpdate(); });
				SystemTime.Now = () => DateTime.Now.AddDays(2);
			}
			billing.ProcessPayments();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsTrue(client.Disabled);
			}
			for (int i = 0; i < countDays; i++) {
				billing.ProcessPayments();
				billing.ProcessWriteoffs();
				int i1 = i;
				SystemTime.Now = () => DateTime.Now.AddDays(i1 + 3);
			}
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.AsQueryable().Where(c => c.Client == client).ToList().Sum(w => w.WriteOffSum), Is.EqualTo(0));
			}

			SystemTime.Now = () => DateTime.Now.AddDays(countDays + 3);
			billing.ProcessPayments();
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.IsFalse(client.Disabled);
				Assert.That(client.ClientServices.Count, Is.EqualTo(2), "должна остаться только услуга internet");
			}
			SystemTime.Now = () => service.EndWorkDate.Value.AddDays(46);
			billing.ProcessPayments();

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
				service.TryActivate();
				client.Update();
				countDays = 0;

				client.FreeBlockDays = 28;
				client.Update();
				balance = client.PhysicalClient.Balance;
				ArHelper.WithSession(s => { s.CreateSQLQuery("delete from Internet.WriteOff").ExecuteUpdate(); });
			}
			while (balance > 0) {
				billing.ProcessPayments();
				billing.ProcessWriteoffs();
				SystemTime.Now = () => DateTime.Now.AddDays(countDays);
				countDays++;
				using (new SessionScope()) {
					client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
					balance = client.PhysicalClient.Balance;
				}
			}
			using (new SessionScope()) {
				var writeOffs = WriteOff.Queryable.AsEnumerable().Where(w => w.Client.Id == client.Id && w.Sum == 3m).ToList();
				var sum = writeOffs.Sum(w => w.WriteOffSum);
				var lastWriteoffAt = writeOffs.Max(w => w.WriteOffDate);
				var firstWriteoffAt = writeOffs.Min(w => w.WriteOffDate);
				var expectedSum = ((lastWriteoffAt - firstWriteoffAt).Days + 1) * 3;

				Assert.That(expectedSum, Is.EqualTo(Math.Round(sum)));
				Assert.That(firstWriteoffAt.Date, Is.EqualTo(DateTime.Now.AddDays(29).Date));
				writeOffs.Select(w => w.WriteOffSum).Each(c => Assert.That(c, Is.EqualTo(3m)));
			}
		}

		[Test]
		public void ActiveDeactive()
		{
			ClientService service;
			PhysicalClient physClient;
			var currentDate = SystemTime.Now();
			const int countDays = 3;
			using (new SessionScope()) {
				physClient = client.PhysicalClient;
				physClient.Balance = -10m;
				physClient.Update();
				client.Disabled = true;
				client.AutoUnblocked = true;
				client.RatedPeriodDate = currentDate;
				client.Update();

				service = Activate(typeof(DebtWork), SystemTime.Now().AddDays(countDays));
			}
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsFalse(client.Disabled);
				service.ForceDeactivate();
				Assert.IsTrue(client.Disabled);
				billing.ProcessPayments();
				Assert.IsTrue(client.Disabled);
				physClient = ActiveRecordMediator<PhysicalClient>.FindByPrimaryKey(physClient.Id);
				physClient.Balance = client.GetPriceForTariff();
				physClient.Update();
			}
			billing.ProcessPayments();
			billing.ProcessPayments();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsFalse(client.Disabled);
				Assert.IsFalse(client.PaidDay);
				Assert.IsTrue(client.AutoUnblocked);
				Assert.AreNotEqual(client.RatedPeriodDate, currentDate); //дата не зануляется, а обновляется (клиент должен платить абонентскую плату и без лизы)

				service = new ClientService {
					Client = client,
					BeginWorkDate = DateTime.Now,
					EndWorkDate = SystemTime.Now().AddDays(countDays),
					Service = Service.GetByType(typeof(VoluntaryBlockin)),
				};
				client.ClientServices.Add(service);
				service.TryActivate();
				Assert.IsTrue(client.Disabled);
			}
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				service = ActiveRecordMediator<ClientService>.FindByPrimaryKey(service.Id);
				Assert.That(WriteOff.FindAll().Last().WriteOffSum, Is.GreaterThan(3m));
				service.ForceDeactivate();
			}
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client = Client.Find(client.Id);
				var userWriteOffs = UserWriteOff.Queryable.ToList();
				Assert.That(userWriteOffs.Count, Is.EqualTo(3), userWriteOffs.Implode());
				Assert.That(userWriteOffs[0].Sum, Is.GreaterThan(5m));
				Assert.That(userWriteOffs[0].Sum, Is.LessThan(25m));
				Assert.That(userWriteOffs[1].Sum, Is.EqualTo(50m));
				Assert.That(userWriteOffs[2].Sum, Is.GreaterThan(5m));
				Assert.That(userWriteOffs[2].Sum, Is.LessThan(25m));
				Assert.IsFalse(client.Disabled);
			}
			billing.ProcessPayments();
			using (new SessionScope()) {
				client.Refresh();
			}
			Assert.IsFalse(client.Disabled);
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
					Service = ActiveRecordMediator<DebtWork>.FindFirst(),
					IsActivated = true
				};
				ActiveRecordMediator.Save(service);

				client.Refresh();
				Assert.IsFalse(client.CanBlock());
				client.ClientServices.Remove(service);

				service = new ClientService {
					Client = client,
					Service = Service.GetByType(typeof(DebtWork)),
					BeginWorkDate = DateTime.Now,
					EndWorkDate = DateTime.Now.AddDays(1),
					IsActivated = true
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
			billing.ProcessWriteoffs();
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
			billing.ProcessPayments();
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
				service.TryActivate();
			}
			billing.ProcessPayments();
			billing.ProcessWriteoffs();

			using (new SessionScope()) {
				client_simple.Refresh();
				client_Post.Refresh();
				Assert.IsFalse(client_Post.Disabled);
				Assert.IsFalse(client_simple.Disabled);
				SystemTime.Now = () => DateTime.Now.AddHours(25);
			}
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				Assert.That(WriteOff.Queryable.AsQueryable().Where(w => w.Client == client_Post).Count(), Is.EqualTo(2));

				client_simple.Refresh();
				client_Post = ActiveRecordMediator<Client>.FindByPrimaryKey(client_Post.Id);
				Assert.IsTrue(client_Post.Disabled);
				Assert.IsFalse(client_simple.Disabled);
				client_Post.Disabled = false;
				client_Post.Update();
			}
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client_simple.Refresh();
				client_Post.Refresh();
				Assert.IsTrue(client_Post.Disabled, client_Post.Id.ToString());
				Assert.IsFalse(client_simple.Disabled);

				client_Post.Refresh();
			}
		}

		[Test]
		public void WorkLawyerTestWarningPage()
		{
			var warningParamRaw = ConfigurationManager.AppSettings["LawyerPersonBalanceWarningRate"];
			var warningParam = (decimal)float.Parse(warningParamRaw, CultureInfo.InvariantCulture);

			var serviceSum = 1000;
			Client client;
			using (new SessionScope()) {
				client = new Client() {
					Status = ArHelper.WithSession(s => Status.Get(StatusType.Worked, s)),
					LawyerPerson = new LawyerPerson {
						Name = "testLawyerPerson",
						Balance = (-serviceSum * warningParam) - 1,
						Region = ArHelper.WithSession(s => s.Query<RegionHouse>().FirstOrDefault())
					}
				};
				client.Save();
				var order = new Order(client.LawyerPerson) { BeginDate = DateTime.Now.AddDays(-4) };

				var service = new OrderService(order, serviceSum, true);
				order.Client = client;
				order.OrderServices.Add(service);
				ArHelper.WithSession(s => s.Save(service));
				ArHelper.WithSession(s => s.Save(order));
				client.Orders.Add(order);
				client.Save();
			}
			billing.SafeProcessClientEndpointSwitcher();
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsTrue(client.ShowBalanceWarningPage);
				Assert.False(client.Disabled);
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
			billing.SafeProcessClientEndpointSwitcher();
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client.Refresh();
				Assert.False(client.ShowBalanceWarningPage);
				Assert.False(client.Disabled);
				Assert.That(client.ClientServices.Count, Is.EqualTo(1));
			}
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.SafeProcessClientEndpointSwitcher();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client.Refresh();
				Assert.False(client.ShowBalanceWarningPage);
				Assert.That(client.ClientServices.Count, Is.EqualTo(1));
			}
			SystemTime.Now = () => DateTime.Now.AddDays(2);
			billing.SafeProcessClientEndpointSwitcher();
      billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.That(client.ClientServices.Count, Is.EqualTo(0));
				Assert.True(client.ShowBalanceWarningPage);
				Assert.False(client.Disabled);
			}
		}

		[Test]
		public void WorkLawyerTestDisabled()
		{
			var serviceSum = 1000;

			var disableParamRaw = ConfigurationManager.AppSettings["LawyerPersonBalanceBlockingRate"];
			var disableParam = (decimal)float.Parse(disableParamRaw, CultureInfo.InvariantCulture);


			Client client;
			using (new SessionScope()) {
				client = new Client() {
					Status = ArHelper.WithSession(s => Status.Get(StatusType.Worked, s)),
					LawyerPerson = new LawyerPerson {
						Name = "testLawyerPerson",
						Balance = -serviceSum * disableParam,
						Region = ArHelper.WithSession(s => s.Query<RegionHouse>().FirstOrDefault())
					}
				};
				client.Save();
				var order = new Order(client.LawyerPerson) { BeginDate = DateTime.Now.AddDays(-4) };

				var service = new OrderService(order, serviceSum, true);
				order.Client = client;
				order.OrderServices.Add(service);
				ArHelper.WithSession(s => s.Save(service));
				ArHelper.WithSession(s => s.Save(order));
				client.Orders.Add(order);
				client.Save();
			}
			billing.SafeProcessClientEndpointSwitcher();
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client.Refresh();
				Assert.IsTrue(client.ShowBalanceWarningPage);
				Assert.IsTrue(client.Disabled);
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
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client.Refresh();
				Assert.False(client.ShowBalanceWarningPage);
				Assert.False(client.Disabled);
				Assert.That(client.ClientServices.Count, Is.EqualTo(1));
			}
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client.Refresh();
				Assert.False(client.ShowBalanceWarningPage);
				Assert.That(client.ClientServices.Count, Is.EqualTo(1));
			}
			SystemTime.Now = () => DateTime.Now.AddDays(2);
			billing.ProcessWriteoffs();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				Assert.That(client.ClientServices.Count, Is.EqualTo(0));
				Assert.True(client.ShowBalanceWarningPage);
				Assert.True(client.Disabled);
			}
		}

		private void AssertThatContainsOnlyMandatoryServices()
		{
			Assert.That(client.ClientServices.Count, Is.EqualTo(2), client.ClientServices.Implode(c => c.Service));
		}

		[Test(Description = "проверяю, что не выдается исключение при данных условиях")]
		public void Tv_box_and_no_rated_date()
		{
			using (new SessionScope()) {
				Activate(typeof(VoluntaryBlockin), DateTime.Now.AddDays(100));
				var clientService = new ClientService(client, Service.GetByType(typeof(IpTvBoxRent)));
				client.Activate(clientService);
				client.PaidDay = false;
				client.RatedPeriodDate = null;
				ActiveRecordMediator.Save(client);
			}
			billing.ProcessPayments();
			billing.ProcessWriteoffs();
			using (new SessionScope()) {
				var settings = ActiveRecordMediator<InternetSettings>.FindFirst();
				Assert.IsFalse(settings.LastStartFail);
			}
		}
	}
}