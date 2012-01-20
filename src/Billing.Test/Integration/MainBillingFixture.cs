#define BILLING_TEST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Castle.ActiveRecord;
using Common.Tools;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;
using NUnit.Framework;
using InternetInterface.Models;


namespace Billing.Test.Integration
{
	[TestFixture]
	public class IntegrationFixture
	{
		protected ISessionScope scope;

		[SetUp]
		public void Setup()
		{
			scope = new SessionScope();
		}

		[TearDown]
		public void TearDown()
		{
			if (scope != null)
				scope.Dispose();
		}
	}

	public class MainBillingFixture : IntegrationFixture
	{
		protected MainBilling billing;
		protected Client _client;

		[SetUp]
		public void CreateBilling()
		{
			billing = new MainBilling();
			PrepareTest();
			_client = CreateClient();
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
				Name = "unblocked"
			}.Save();

			
			new Status
			{
				Blocked = true,
				Id = (uint)StatusType.BlockedAndConnected,
				Name = "unblocked"
			}.Save();

			new Status
			{
				Blocked = true,
				Id = (uint)StatusType.NoWorked,
				Name = "testBlockedStatus"
			}.Save();

			new Status {
						   ShortName = "VoluntaryBlocking",
						   Id = (uint)StatusType.VoluntaryBlocking,
						   Blocked = true,
						   Connected = true,
					   }.Save();

			new DebtWork
			{
				BlockingAll = false,
				Price = 0
			}.Save();

			new VoluntaryBlockin
			{
				BlockingAll = true,
				Price = 0
			}.Save();

			new InternetSettings{NextBillingDate = DateTime.Now}.Save();
		}

		public Client CreateClient()
		{
			var client = BaseBillingFixture.CreateAndSaveClient("testClient1", false, 590);
			client.Save();
			return client;
		}

		public static void PrepareTest()
		{
			SmsMessage.DeleteAll();
			UserWriteOff.DeleteAll();
			ClientService.DeleteAll();
			WriteOff.DeleteAll();
			Payment.DeleteAll();
			Client.DeleteAll();
			PhysicalClients.DeleteAll();
			SystemTime.Reset();
		}

		public void SetClientDate(Client client, Interval rd)
		{
			client = Client.FindFirst();
			client.RatedPeriodDate = rd.dtFrom;
			client.Update();
			SystemTime.Now = () => rd.dtTo;
			billing.Compute();
		}
	}
}
