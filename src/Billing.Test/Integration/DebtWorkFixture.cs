using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Models;
using InternetInterface.Services;
using NUnit.Framework;

namespace Billing.Test.Integration
{
	public class DebtWorkFixture : BillingFixture2
	{
		[Test]
		public void DebtWorkActivateDiactivate()
		{
			client.AutoUnblocked = true;
			client.Disabled = true;
			client.PhysicalClient.Balance = -5m;

			var service = new ClientService(client, Service.GetByType(typeof(DebtWork))) {
				BeginWorkDate = DateTime.Now,
				EndWorkDate = DateTime.Now.AddDays(1),
			};
			client.Activate(service);
			Assert.IsFalse(client.Disabled);
			SystemTime.Now = () => DateTime.Now.AddDays(2);
			service.Deactivate();
			Assert.IsTrue(client.Disabled);
			service.TryActivate();
			Assert.IsTrue(client.Disabled);
		}

		[Test]
		public void Debt_Work_diactivate_and_delete()
		{
			client.AutoUnblocked = true;
			client.Disabled = true;
			client.PhysicalClient.Balance = -5m;
			session.Save(client);
			var service = new ClientService(client, Service.GetByType(typeof(DebtWork))) {
				BeginWorkDate = DateTime.Now,
				EndWorkDate = DateTime.Now.AddDays(1),
			};
			client.Activate(service);
			Assert.IsTrue(client.ClientServices.Select(c => c.Service).Contains(Service.Type<DebtWork>()));

			SystemTime.Now = () => DateTime.Now.AddDays(2);
			billing.OnMethod();

			session.Refresh(client);
			service = client.ClientServices.FirstOrDefault(s => s.Service.Id == Service.Type<DebtWork>().Id);
			Assert.IsNotNull(service);
			Assert.IsTrue(service.Diactivated);
			Assert.False(service.Activated);

			session.Save(new Payment(client, client.GetPriceForTariff() + 50));
			billing.OnMethod();

			session.Refresh(client);
			service = client.ClientServices.FirstOrDefault(s => s.Service.Id == Service.Type<DebtWork>().Id);
			Assert.IsNull(service);
		}

		[Test]
		public void Debt_work_and_payment()
		{
			client.AutoUnblocked = true;
			client.PhysicalClient.Balance = 1;
			session.Save(client);
			session.Transaction.Commit();

			billing.Compute();

			session.Refresh(client);
			Assert.IsTrue(client.Disabled);
			var service = new ClientService(client, Service.GetByType(typeof(DebtWork))) {
				BeginWorkDate = DateTime.Now,
				EndWorkDate = DateTime.Now.AddDays(1),
			};
			client.Activate(service);
			session.Save(new Payment(client, 550));
			billing.On();
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.On();

			session.Refresh(client);
			Assert.That(client.Disabled, Is.False);
			Assert.That(client.Status.Id, Is.EqualTo((uint)StatusType.Worked));
		}

		[Test]
		public void New_client_debt_work()
		{
			client.AutoUnblocked = true;
			client.Disabled = true;
			client.BeginWork = null;
			client.PhysicalClient.Balance = 0;
			session.Save(client);
			var service = new ClientService(client, Service.GetByType(typeof(DebtWork))) {
				BeginWorkDate = DateTime.Now,
				EndWorkDate = DateTime.Now.AddDays(3),
			};
			client.Activate(service);
			billing.OnMethod();

			session.Refresh(client);
			Assert.IsFalse(client.Disabled);
		}
	}
}