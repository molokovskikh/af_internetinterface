using System;
using System.Linq;
using Common.Tools;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models.Services
{
	[Subclass(0, ExtendsType = typeof(Service), DiscriminatorValue = "DebtWork")]
	public class DeferredPayment : Service
	{
		public override bool CanActivate(ClientService assignedService)
		{
			var client = assignedService.Client;
			return !assignedService.IsActivated && !assignedService.IsDeactivated && IsAvailableInThisTime(client);
		}

		public override void Activate(ClientService assignedService, ISession session)
		{
			if (CanActivate(assignedService)) {
				var client = assignedService.Client;
				client.RatedPeriodDate = SystemTime.Now();
				client.SetStatus(StatusType.Worked, session);
				session.Update(client);
				assignedService.IsActivated = true;
			}
		}

		// Причина, по которой клиенту не доступен "Обещанный платеж" (заполняется при вызове метода IsAvailableInThisTime)
		public virtual string NotActivateReason { get; protected set; }

		// Метод для проверки, доступен ли клиенту "Обещанный платеж" в данный момент времени
		public virtual bool IsAvailableInThisTime(Client client)
		{
			NotActivateReason = "";
			var lastService = client.ClientServices.OrderBy(cs => cs.BeginDate).LastOrDefault(s => s.Service.Id == Id);
			if (lastService == null) // Т.е. "Обещанный платеж" ещё ни разу не активировался 
				return true;
			if (lastService.IsActivated) {
				NotActivateReason = "Данная услуга у вас уже активирована.";
				return false;
			}

			var serviceDate = lastService.BeginDate ?? new DateTime();
			// Проверка, запрещающая клиенту повторно подключить услугу без пополнения баланса на 80% от цены тарифа (СОГЛАСОВАНО)
			if (serviceDate != DateTime.MinValue) {
				var clientPayments =
					(client.Payments != null) ? client.Payments.Where(p => p.PaidOn.Date >= serviceDate.Date).ToList() : null;
				var paySum = (clientPayments != null) ? clientPayments.Sum(p => p.Sum) : 0;
				var minSum = 0.8m * client.Plan.Price;
				if (paySum < minSum) {
					NotActivateReason = string.Format("С последнего подключения услуги от {0} не пополнялся баланс на сумму абонентской платы тарифа.",
						serviceDate.Date.ToShortDateString());
					return false;
				}
			}

			var timeToActivation = serviceDate.AddDays(30) - DateTime.Now;
			if (timeToActivation > TimeSpan.Zero) {
				NotActivateReason = "Услуга станет доступна через " + StringHelper.ShowTimeLeft(timeToActivation);
				return false;
			}
			return true;
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