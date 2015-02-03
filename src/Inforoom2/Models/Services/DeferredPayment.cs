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
				client.Status = Status.Get(StatusType.Worked, session);
				session.Update(client);
				assignedService.IsActivated = true;
			}
		}

		public override bool IsActivableFor(Client client)
		{
			return client != null
			       && client.Disabled
			       && client.Balance <= 0
			       && !client.HasActiveService<BlockAccountService>()
			       && !client.HasActiveService<DeferredPayment>()
						 && client.ServiceAccepted(this)
			       && client.AutoUnblocked;
		}
	}
}