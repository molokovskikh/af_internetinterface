using System;
using System.Linq;
using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Models;
using InternetInterface.Services;
using NHibernate.Linq;
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
			service.TryDeactivate();
			Assert.IsTrue(client.Disabled);
			service.TryActivate();
			Assert.IsTrue(client.Disabled);
			session.Clear();
		}

		[Test(Description = "Проверка на деактивацию сервиса. Сервис должен деактивироваться, но не удаляться.")]
		public void TestDebtWorkDeactivate()
		{
			client.PhysicalClient.Balance = -10;
			session.Save(client);
			session.Flush();
			
			var service = session.Query<DebtWork>().First();
			var clientService = new ClientService(client, service);
			clientService.IsDeactivated = false;
			clientService.IsActivated = true;
			clientService.BeginWorkDate = DateTime.Parse("2014-11-07 22:04:13").AddDays(-10);
			clientService.EndWorkDate = clientService.BeginWorkDate.Value.AddDays(3);
			session.Save(clientService);
			session.Refresh(client);
			Assert.That(client.ClientServices.Count, Is.EqualTo(3));
			var serviceId = clientService.Id;
			var clientId = client.Id;

			//После первого пробега биллинга, сервис должен быть деактивирован
			billing.ProcessPayments();
			session.Clear();
			client = session.Get<Client>(clientId);
			clientService = session.Query<ClientService>().FirstOrDefault(i => i.Id == serviceId);
			Assert.That(clientService, Is.Not.Null);
			Assert.That(clientService.IsActivated, Is.False);
			Assert.That(client.ClientServices.Count, Is.EqualTo(3));

			var payment = new Payment(client, 250);
			payment.RecievedOn = DateTime.Parse("2014-11-07 22:04:13");
			payment.PaidOn = DateTime.Parse("2014-11-07 22:04:13");
			payment.BillingAccount = false;
			payment.Virtual = true;
			session.Save(payment);
			session.Flush();

			//После второго пробега биллинга, сервис должен оставаться у клиента - больше мы сервисы не удаляем
			billing.ProcessPayments();
			session.Clear();
			client = session.Get<Client>(clientId);
			payment = client.Payments.First();
			Assert.That(payment.BillingAccount, Is.True);
			Assert.That(client.ClientServices.Count, Is.EqualTo(3));
		}

		[Test(Description = "Проверка, что сервис будет активироваться второй раз")]
		public void SecondActivationOfService()
		{
			client.PhysicalClient.Balance = -10;
			session.Save(client);
			session.Flush();

			var service = session.Query<DebtWork>().First();
			var clientService = new ClientService(client, service);
			clientService.IsDeactivated = true;
			clientService.IsActivated = false;
			clientService.BeginWorkDate = SystemTime.Now().AddDays(-30);
			clientService.EndWorkDate = clientService.BeginWorkDate.Value.AddDays(3);
			session.Save(clientService);
			
			billing.ProcessWriteoffs();
			//Убеждаемся, что клиент находится в том виде, что нам нужно
			session.Refresh(client);
			session.Refresh(clientService);
			Assert.That(client.ClientServices.Count, Is.EqualTo(3));
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.NoWorked));

			clientService = new ClientService(client, service);
			clientService.BeginWorkDate = SystemTime.Now();
			clientService.EndWorkDate = clientService.BeginWorkDate.Value.AddDays(3);
			client.Activate(clientService);

			//Теперь клиент должен разблокироваться
			billing.ProcessWriteoffs();
			session.Refresh(client);
			Assert.That(client.Status.Type, Is.EqualTo(StatusType.Worked));
			Assert.That(client.ClientServices.Count, Is.EqualTo(4));
		}

		[Test]
		public void Debt_work_and_payment()
		{
			client.AutoUnblocked = true;
			client.PhysicalClient.Balance = 1;
			session.Save(client);
			session.Transaction.Commit();

			billing.ProcessWriteoffs();

			session.Refresh(client);
			Assert.IsTrue(client.Disabled);
			var service = new ClientService(client, Service.GetByType(typeof(DebtWork))) {
				BeginWorkDate = DateTime.Now,
				EndWorkDate = DateTime.Now.AddDays(1),
			};
			client.Activate(service);
			session.Save(new Payment(client, 550));
			billing.SafeProcessPayments();
			SystemTime.Now = () => DateTime.Now.AddDays(1);
			billing.SafeProcessPayments();

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
			billing.ProcessPayments();

			session.Refresh(client);
			Assert.IsFalse(client.Disabled);
		}
	}
}