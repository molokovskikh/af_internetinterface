﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Tools;
using InternetInterface.Models;
using InternetInterface.Services;
using InternetInterface.Test.Helpers;
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
		}

		[Test]
		public void Get_writeoff()
		{
			var client = ClientHelper.Client();
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

		[Test]
		public void Do_not_activate_debt_work_second_time()
		{
			var client = ClientHelper.Client();
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
	}
}
