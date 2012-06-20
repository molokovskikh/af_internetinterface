using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Services;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "Internet")]
	public class Internet : Service
	{
		public override bool CanDelete(ClientService assignedService)
		{
			return false;
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			if (!assignedService.ActivatedByUser
				|| assignedService.Client.Disabled)
				assignedService.Activated = false;
			return false;
		}

		public override void Activate(ClientService assignedService)
		{
			if (assignedService.ActivatedByUser
				&& !assignedService.Client.Disabled)
				assignedService.Activated = true;
			assignedService.Activated = true;
		}

		public override decimal GetPrice(ClientService assignedService)
		{
			if (assignedService.Client.Disabled
				|| !assignedService.ActivatedByUser)
				return 0;
			return assignedService.Client.GetTariffPrice();
		}
	}
}