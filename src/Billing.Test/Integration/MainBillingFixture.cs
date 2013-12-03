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

namespace Billing.Test.Integration
{
	public class MainBillingForTest : MainBilling
	{
		public override void Compute()
		{
			base.Compute();

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
					var settings = new SaleSettings {
						MaxSale = MaxSale,
						MinSale = MinSale,
						PeriodCount = PerionCount,
						SaleStep = SaleStep
					};
					s.Save(settings);
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
			InitializeContent.GetAdministrator = () => {
				var partner = Partner.FindFirst();
				partner.AccesedPartner = CategorieAccessSet.FindAll(DetachedCriteria.For(typeof(CategorieAccessSet))
					.CreateAlias("AccessCat", "AC", JoinType.InnerJoin)
					.Add(Restrictions.Eq("Categorie", partner.Categorie)))
					.Select(c => c.AccessCat.ReduceName).ToList();
				return partner;
			};

			ArHelper.WithSession(s => {
				s.CreateSQLQuery("delete from Internet.InternetSettings").ExecuteUpdate();
			});

			using (new SessionScope()) {
				new Partner("Test").Save();
				if (!ActiveRecordLinqBase<Status>.Queryable.Any())
					CreateStatuses();

				new DebtWork {
					BlockingAll = false,
					Price = 0,
					HumanName = "DebtWork"
				}.Save();

				new AgentTariff {
					ActionName = AgentActions.WorkedClient,
					Sum = 250
				}.Save();

				new AgentTariff {
					ActionName = AgentActions.AgentPayIndex,
					Sum = 1.5m
				}.Save();

				new VoluntaryBlockin {
					BlockingAll = true,
					Price = 0,
					HumanName = "VoluntaryBlockin"
				}.Save();

				new WorkLawyer {
					InterfaceControl = true,
					HumanName = "WorkLawyer"
				}.Save();

				new InternetSettings { NextBillingDate = DateTime.Now }.Save();
			}
		}

		private static void CreateStatuses()
		{
			new Status {
				Blocked = false,
				Id = (uint)StatusType.Worked,
				Name = "unblocked",
				ShortName = "Worked"
			}.Save();

			new Status {
				Blocked = true,
				Id = (uint)StatusType.BlockedAndConnected,
				Name = "unblocked",
				ShortName = "BlockedAndConnected"
			}.Save();

			new Status {
				Blocked = true,
				Id = (uint)StatusType.NoWorked,
				Name = "testBlockedStatus",
				ShortName = "NoWorked"
			}.Save();

			new Status {
				ShortName = "VoluntaryBlocking",
				Id = (uint)StatusType.VoluntaryBlocking,
				Blocked = true,
				Name = "VoluntaryBlocking",
				Connected = true
			}.Save();
		}

		public static Client CreateClient()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 590);
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
				s.CreateSQLQuery("delete from Internet.Tariffs").ExecuteUpdate();
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
			billing.Compute();
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
}