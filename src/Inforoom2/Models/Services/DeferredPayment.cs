using System;
using System.Linq;
using Common.Tools;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models.Services
{
	[Subclass(0, ExtendsType = typeof (Service), DiscriminatorValue = "DebtWork")]
	public class DeferredPayment : Service
	{
		public override void Activate(ClientService assignedService, ISession session)
		{
			if (!assignedService.IsActivated && !assignedService.IsDeactivated && !assignedService.Client.ClientServices
				.Except(new[] {assignedService})
				.Any(s => s.IsService(assignedService.Service))) {
				var client = assignedService.Client;
				client.Disabled = false;
				client.RatedPeriodDate = SystemTime.Now();
				client.SetStatus(StatusType.Worked, session);
				session.Update(client);
				assignedService.IsActivated = true;
			}
		}

		// Время до момента, когда клиенту станет доступен "Обещанный платеж" (заполняется при вызове метода IsAvailableInThisTime)
		public virtual TimeSpan TimeToActivation { get; protected set; }

		// Метод для проверки, доступен ли клиенту "Обещанный платеж" в данный момент времени
		public virtual bool IsAvailableInThisTime(Client client)
		{
			var lastService = client.ClientServices.OrderBy(cs => cs.BeginDate).LastOrDefault(s => s.Service.Id == Id);
			if (lastService == null)							// Т.е. "Обещанный платеж" ещё ни разу не активировался
				return true;
			if (lastService.IsActivated)
				return false;
			var serviceDate = lastService.BeginDate ?? new DateTime();
			TimeToActivation = serviceDate.AddDays(30) - DateTime.Now;
			return (TimeToActivation < TimeSpan.Zero);
		}

		public override bool IsActivableFor(Client client)
		{
			return client != null
			       && client.Disabled
			       && client.Balance <= 0
			       && !client.HasActiveService<BlockAccountService>()
			       && !client.HasActiveService<DeferredPayment>()
			       && IsAvailableInThisTime(client)
			       && client.AutoUnblocked;
		}
	}
}