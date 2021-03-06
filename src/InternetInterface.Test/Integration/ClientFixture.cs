﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Billing;
using Castle.Components.Validator;
using Common.Tools;
using Common.Web.Ui.MonoRailExtentions;
using InternetInterface.Models;
using InternetInterface.Services;
using InternetInterface.Test.Helpers;
using NHibernate.Linq;
using NUnit.Framework;
using Test.Support;

namespace InternetInterface.Test.Integration
{
	[TestFixture]
	public class ClientFixture : IntegrationFixture
	{
		[TearDown]
		public void TearDown()
		{
			SystemTime.Reset();
			ValidEventListner.ValidatorAccessor = null;
		}

		[Test]
		public void Get_writeoff()
		{
			var client = ClientHelper.Client(session);
			session.SaveOrUpdate(client);
			var writeOff = new WriteOff(client, 500m);
			var userWriteOff = new UserWriteOff(client, 100m, "testUserWriteOff");
			session.Save(writeOff);
			session.Save(userWriteOff);
			Flush();
			var writeOffs = client.GetWriteOffs(session, string.Empty);
			Assert.AreEqual(writeOffs.Count, 2);
			Assert.That(writeOffs[0].WriteOffSum, Is.EqualTo(500m));
			Assert.That(writeOffs[1].WriteOffSum, Is.EqualTo(100m));
			Assert.That(writeOffs[1].Comment, Is.EqualTo("testUserWriteOff"));
		}

		[Test, Ignore("Бессмысленный тест, так как из старого интернет интерфейса ничего больше подключать не надо")]
		public void Do_not_activate_debt_work_second_time()
		{
			var client = ClientHelper.Client(session);
			client.BeginWork = DateTime.Now;
			session.Save(client);
			client.WriteOff(500, false);
			client.UpdateStatus();

			Assert.IsTrue(client.CanUseDebtWork());
			var clientService = new ClientService(client, Service.Type<DebtWork>()) {
				BeginWorkDate = DateTime.Now,
				EndWorkDate = DateTime.Now.AddDays(1)
			};
			client.Activate(clientService);
			session.Save(client);
			session.Flush();

			SystemTime.Now = () => DateTime.Now.AddDays(2);
			client.ClientServices.Each(s => s.TryDeactivate());
			Assert.IsTrue(clientService.IsDeactivated);
			Assert.IsTrue(client.Disabled);

			Assert.IsFalse(client.CanUseDebtWork());
			Assert.Throws<ServiceActivationException>(() => client.Activate(new ClientService(client, Service.Type<DebtWork>())));
		}

		[Test(Description = "После деактивации услуги добровольная блокировка у клиента отрицательный баланс мы должны сбросить скидку тк он ушел в минус"), Ignore("Тест был перенесен. Inforoom2.Test - WriteoffBlockingPayWithClientDiscount() ")]
		public void Reset_sale_on_disable_in_voluntary_block()
		{
			MainBilling.InitActiveRecord();
			session.BeginTransaction();
			var status = session.Query<Status>().First(s=>s.Id == (int)StatusType.Worked);
			var settings = session.Query<SaleSettings>().First();
			var client = ClientHelper.Client(session);
			client.RatedPeriodDate = DateTime.Now;
			client.BeginWork = DateTime.Now.AddYears(-2);
			client.StartNoBlock = DateTime.Now.AddYears(-2);
			client.Sale = settings.MaxSale;
			client.PhysicalClient.Balance = 50;
			client.PhysicalClient.MoneyBalance = 0;
			client.FreeBlockDays = 0;
			client.Status = status;
			session.Save(client);
			session.Flush();
			session.Transaction.Commit();

			var billing = new MainBilling();
			billing.ProcessPayments();

			session.BeginTransaction();
			client = session.Query<Client>().First(s => s.Id == client.Id);
			var sumForRegularWriteOff = client.GetSumForRegularWriteOff();
			client.PhysicalClient.Balance = sumForRegularWriteOff + 50;
			var clientService = new ClientService(client, session.Query<VoluntaryBlockin>().First()) {
				BeginWorkDate = DateTime.Today,
				EndWorkDate = DateTime.Today.AddDays(1)
			};
			client = session.Query<Client>().First(s => s.Id == client.Id);
			client.Activate(clientService);
			session.Transaction.Commit();

			billing = new MainBilling();
			billing.ProcessPayments();
			billing.ProcessWriteoffs();

			session.BeginTransaction();
			client = session.Query<Client>().First(s => s.Id == client.Id);
			session.Refresh(client);
			Assert.That(client.Balance, Is.EqualTo(0));
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			client.ClientServices.ToArray().Each(c => c.TryDeactivate());
			Assert.IsFalse(clientService.IsActivated);
			Assert.IsTrue(client.Disabled);
			Assert.AreEqual(0, client.Sale);
			Assert.IsNull(client.StartNoBlock);
			Assert.AreEqual(StatusType.NoWorked, client.Status.Type);
			session.Transaction.Commit();
		}

		[Test]
		public void Do_not_audit_not_valid_changes()
		{
			var client = ClientHelper.Client(session);
			session.Save(client);
			session.Flush();
			session.Clear();

			client = session.Load<Client>(client.Id);
			client.PhysicalClient.Surname = "";
			var validator = new ValidatorRunner(new CachedValidationRegistry());
			Assert.IsFalse(validator.IsValid(client.PhysicalClient));
			ValidEventListner.ValidatorAccessor = new LambdaValidatorAccessor(() => validator);
			session.Flush();
			session.Clear();

			var appeals = session.Query<Appeals>().Where(a => a.Client == client).ToArray();
			Assert.AreEqual(0, appeals.Length, appeals.Implode());
		}
	}
}
