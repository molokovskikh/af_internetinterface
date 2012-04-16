using System;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Controllers.Filter;
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
				foreach (var paidClient in Client.FindAll()) {
					paidClient.PaidDay = false;
					paidClient.Update();
				}
			}
		}
	}

	public class MainBillingFixture
	{
		protected MainBilling billing;
		protected Client _client;

		protected const int MaxSale = 15;
		protected const int MinSale = 3;
		protected const int PerionCount = 3;
		protected const decimal SaleStep = 1m;

		[SetUp]
		public void CreateBilling()
		{
			using (new SessionScope()) {
				billing = new MainBillingForTest();
				PrepareTest();
				_client = CreateClient();
				SaleSettings.DeleteAll();
				new SaleSettings {
					MaxSale = MaxSale,
					MinSale = MinSale,
					PeriodCount = PerionCount,
					SaleStep = SaleStep
				}.Save();
			}
		}

		public static void PrepareTests()
		{
			new Partner
			{
				Login = "Test",
			}.Save();

			InitializeContent.GetAdministrator = () => Partner.FindFirst();

			new Status
			{
				Blocked = false,
				Id = (uint)StatusType.Worked,
				Name = "unblocked",
				ShortName = "Worked"
			}.Save();

			
			new Status
			{
				Blocked = true,
				Id = (uint)StatusType.BlockedAndConnected,
				Name = "unblocked",
				ShortName = "BlockedAndConnected"
			}.Save();

			new Status
			{
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

			new DebtWork
			{
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

			new VoluntaryBlockin
			{
				BlockingAll = true,
				Price = 0,
				HumanName = "VoluntaryBlockin"
			}.Save();
			InternetSettings.DeleteAll();
			new InternetSettings{NextBillingDate = DateTime.Now}.Save();
		}

		public static Client CreateClient()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 590);
			client.Save();
			return client;
		}

		public static void PrepareTest()
		{
			Request.DeleteAll();
			SmsMessage.DeleteAll();
			UserWriteOff.DeleteAll();
			ClientService.DeleteAll();
			WriteOff.DeleteAll();
			Payment.DeleteAll();
			Client.DeleteAll();
			PhysicalClients.DeleteAll();
			SystemTime.Reset();
			PaymentsForAgent.DeleteAll();
		}

		public void SetClientDate(/*Client client,*/ Interval rd)
		{
			using (new SessionScope()) {
				var client = Client.FindFirst();
				client.RatedPeriodDate = rd.dtFrom;
				client.Update();
			}
			SystemTime.Now = () => rd.dtTo;
			billing.Compute();
		}
	}
}
