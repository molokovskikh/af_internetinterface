using System;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;
using Common.Web.Ui.ActiveRecordExtentions;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using InternetInterface.Services;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using NHibernate.SqlCommand;
using NUnit.Framework;
using InternetInterface.Models;
using Test.Support;

namespace Billing.Test.Integration
{
	public class MainBillingForTest : MainBilling
	{
		public override void ProcessWriteoffs(CancellationToken token = default(CancellationToken))
		{
			base.ProcessWriteoffs();

			using (new SessionScope()) {
				ArHelper.WithSession(s => s.CreateSQLQuery("update internet.Clients set PaidDay = false;").ExecuteUpdate());
			}
		}
	}

	public class MainBillingFixture
	{
		protected SessionScope scope;
		protected MainBilling billing;
		protected Client client;
		protected ISession session;
		private ISessionFactoryHolder sessionHolder;

		protected const int MaxSale = 15;
		protected const int MinSale = 3;
		protected const int PerionCount = 3;
		protected const decimal SaleStep = 1m;

		[SetUp]
		public void CreateBilling()
		{
			SystemTime.Reset();

			using (new SessionScope()) {
				ArHelper.WithSession(s => {
					billing = new MainBillingForTest();
					CleanDb();
					client = CreateClient();

					s.CreateSQLQuery("delete from Internet.SaleSettings").ExecuteUpdate();
					s.Save(SaleSettings.Defaults());
				});
			}
		}

		[TearDown]
		public void Teardown()
		{
			if (session != null)
				sessionHolder.ReleaseSession(session);

			if (scope != null)
				scope.Dispose();
		}

		public void InitSession()
		{
			scope = new SessionScope();
			sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
			session = sessionHolder.CreateSession(typeof(ActiveRecordBase));
		}

		public static void SeedDb()
		{
			InitializeContent.GetPartner = () => {
				var partner = Partner.FindFirst();
				partner.AccesedPartner = CategorieAccessSet.FindAll(DetachedCriteria.For(typeof(CategorieAccessSet))
					.CreateAlias("AccessCat", "AC", JoinType.InnerJoin)
					.Add(Restrictions.Eq("Categorie", partner.Role)))
					.Select(c => c.AccessCat.ReduceName).ToList();
				return partner;
			};

			ArHelper.WithSession(s => {
				s.CreateSQLQuery("delete from Internet.InternetSettings").ExecuteUpdate();
			});

			using (new SessionScope()) {
				ArHelper.WithSession(s => {
					s.Save(new Partner("Test", s.Load<UserRole>(3u)));
					if (!s.Query<Status>().Any())
						CreateStatuses();

					s.Save(new DebtWork {
						BlockingAll = false,
						Price = 0,
						HumanName = "DebtWork"
					});

					s.Save(new AgentTariff {
						ActionName = AgentActions.WorkedClient,
						Sum = 250
					});

					s.Save(new AgentTariff {
						ActionName = AgentActions.AgentPayIndex,
						Sum = 1.5m
					});

					s.Save(new VoluntaryBlockin {
						BlockingAll = true,
						Price = 0,
						HumanName = "VoluntaryBlockin"
					});

					s.Save(new WorkLawyer {
						InterfaceControl = true,
						HumanName = "WorkLawyer"
					});

					s.Save(new InternetSettings { NextBillingDate = DateTime.Now });
				});
			}
		}

		private static void CreateStatuses()
		{
			new Status {
				Id = (uint)StatusType.Worked,
				Name = "unblocked",
				ShortName = "Worked"
			}.Save();

			new Status {
				Id = (uint)StatusType.BlockedAndConnected,
				Name = "unblocked",
				ShortName = "BlockedAndConnected"
			}.Save();

			new Status {
				Id = (uint)StatusType.NoWorked,
				Name = "testBlockedStatus",
				ShortName = "NoWorked"
			}.Save();

			new Status {
				ShortName = "VoluntaryBlocking",
				Id = (uint)StatusType.VoluntaryBlocking,
				Name = "VoluntaryBlocking",
			}.Save();
		}

		public static Client CreateClient()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 590);

			var endpoint = new ClientEndpoint();
			endpoint.IsEnabled = true;
			endpoint.Client = client;
			client.Endpoints.Add(endpoint);
			endpoint.Save();
			client.Save();
			return client;
		}

		public static void CleanDb()
		{
			ArHelper.WithSession(s => {
				s.CreateSQLQuery(
					@"delete from Internet.ClientServices;
				delete from Internet.Requests;
				delete from Internet.SmsMessages;
				delete from Internet.UserWriteOffs;
				delete from Internet.WriteOff;
				delete from Internet.Payments;
				delete from Internet.Clients;
				delete from Internet.PhysicalClients;
				delete from Internet.PaymentsForAgent;
				delete from Internet.Appeals;
				delete from Internet.LawyerPerson;").ExecuteUpdate();
			});
		}

		public static void CleanDbAfterTest()
		{
			ArHelper.WithSession(s => {
				s.CreateSQLQuery("delete from Internet.inforoom2_planchangerdata; delete from Internet.Tariffs").ExecuteUpdate();
			});
		}

		public void SetClientDate(Interval rd, Client client)
		{
			using (new SessionScope()) {
				client = ActiveRecordMediator<Client>.FindByPrimaryKey(client.Id);
				client.RatedPeriodDate = rd.dtFrom;
				client.Update();
			}
			SystemTime.Now = () => rd.dtTo;
			billing.ProcessWriteoffs();
		}

		public void Assert_statistic_appeal(int appealCount = 1)
		{
			using (new SessionScope()) {
				ArHelper.WithSession(s => {
					var appeals = s.Query<Appeals>().Where(a => a.AppealType == AppealType.Statistic).ToList();
					Assert.That(appeals.Count, Is.EqualTo(appealCount));
					s.CreateSQLQuery("delete from Internet.appeals").ExecuteUpdate();
				});
			}
		}

		protected int Wait<T>(T entity, Func<bool> condition, Action action) where T : ActiveRecordBase
		{
			var timeout = TimeSpan.FromSeconds(30);
			var begin = DateTime.Now;
			var i = 0;
			while (!condition()) {
				if (DateTime.Now - begin > timeout)
					throw new Exception("Не удалось дождаться выполения условия");
				action();
				using (new SessionScope())
					entity.Refresh();
				i++;
			}
			return i;
		}
	}


	public class BillingFixture2 : IntegrationFixture
	{
		protected MainBilling billing;
		protected Client client;

		protected const int MaxSale = 15;
		protected const int MinSale = 3;
		protected const int PerionCount = 3;
		protected const decimal SaleStep = 1m;

		[SetUp]
		public void CreateBilling()
		{
			SystemTime.Reset();

			billing = new MainBillingForTest();
			CleanDb();
			client = CreateClient();

			session.CreateSQLQuery("delete from Internet.SaleSettings").ExecuteUpdate();
			session.Save(SaleSettings.Defaults());
		}

		public void SeedDb()
		{
			InitializeContent.GetPartner = () => {
				var partner = Partner.FindFirst();
				partner.AccesedPartner = CategorieAccessSet.FindAll(DetachedCriteria.For(typeof(CategorieAccessSet))
					.CreateAlias("AccessCat", "AC", JoinType.InnerJoin)
					.Add(Restrictions.Eq("Categorie", partner.Role)))
					.Select(c => c.AccessCat.ReduceName).ToList();
				return partner;
			};

			session.CreateSQLQuery("delete from Internet.InternetSettings").ExecuteUpdate();

			session.Save(new Partner("Test", session.Load<UserRole>(3u)));
			if (!session.Query<Status>().Any())
				CreateStatuses();

			session.Save(new DebtWork {
				BlockingAll = false,
				Price = 0,
				HumanName = "DebtWork"
			});

			session.Save(new AgentTariff {
				ActionName = AgentActions.WorkedClient,
				Sum = 250
			});

			session.Save(new AgentTariff {
				ActionName = AgentActions.AgentPayIndex,
				Sum = 1.5m
			});

			session.Save(new VoluntaryBlockin {
				BlockingAll = true,
				Price = 0,
				HumanName = "VoluntaryBlockin"
			});

			session.Save(new WorkLawyer {
				InterfaceControl = true,
				HumanName = "WorkLawyer"
			});

			session.Save(new InternetSettings { NextBillingDate = DateTime.Now });
		}

		private void CreateStatuses()
		{
			session.Save(new Status {
				Id = (uint)StatusType.Worked,
				Name = "unblocked",
				ShortName = "Worked"
			});

			session.Save(new Status {
				Id = (uint)StatusType.BlockedAndConnected,
				Name = "unblocked",
				ShortName = "BlockedAndConnected"
			});

			session.Save(new Status {
				Id = (uint)StatusType.NoWorked,
				Name = "testBlockedStatus",
				ShortName = "NoWorked"
			});

			session.Save(new Status {
				ShortName = "VoluntaryBlocking",
				Id = (uint)StatusType.VoluntaryBlocking,
				Name = "VoluntaryBlocking",
			});
		}

		public Client CreateClient()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 590);
			session.Save(client);
			return client;
		}

		public void CleanDb()
		{
			session.CreateSQLQuery(@"delete from Internet.ClientServices;
delete from Internet.Requests;
delete from Internet.SmsMessages;
delete from Internet.UserWriteOffs;
delete from Internet.WriteOff;
delete from Internet.Payments;
delete from Internet.Clients;
delete from Internet.PhysicalClients;
delete from Internet.PaymentsForAgent;
delete from Internet.Appeals;
delete from Internet.LawyerPerson;")
				.ExecuteUpdate();
		}

		public void CleanDbAfterTest()
		{
			session.CreateSQLQuery("delete from Internet.Tariffs").ExecuteUpdate();
		}

		public void SetClientDate(Interval rd, Client client)
		{
			client = session.Load<Client>(client.Id);
			client.RatedPeriodDate = rd.dtFrom;
			client.Update();
			SystemTime.Now = () => rd.dtTo;
			billing.ProcessWriteoffs();
		}

		public void Assert_statistic_appeal(int appealCount = 1)
		{
			var appeals = session.Query<Appeals>().Where(a => a.AppealType == AppealType.Statistic).ToList();
			Assert.That(appeals.Count, Is.EqualTo(appealCount));
			session.CreateSQLQuery("delete from Internet.appeals").ExecuteUpdate();
		}

		protected int Wait<T>(T entity, Func<bool> condition, Action action) where T : ActiveRecordBase
		{
			var timeout = TimeSpan.FromSeconds(30);
			var begin = DateTime.Now;
			var i = 0;
			while (!condition()) {
				if (DateTime.Now - begin > timeout)
					throw new Exception("Не удалось дождаться выполения условия");
				action();
				using (new SessionScope())
					entity.Refresh();
				i++;
			}
			return i;
		}
	}
}