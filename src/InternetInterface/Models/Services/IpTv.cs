using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using InternetInterface.Services;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "IpTv")]
	public class IpTv : Service
	{
		public override bool CanDelete(ClientService assignedService)
		{
			return false;
		}

		public override bool CanDeactivate(ClientService assignedService)
		{
			return assignedService.Client.Disabled && assignedService.Activated;
		}

		public override void Activate(ClientService assignedService)
		{
			if (!assignedService.Client.Disabled)
				base.Activate(assignedService);
		}

		public override decimal GetPrice(ClientService assignedService)
		{
			if (assignedService.Client.Disabled)
				return 0;

			if (assignedService.Client.HaveService<Internet>())
				return assignedService.Channels.Sum(c => c.CostPerMonthWithInternet);

			return assignedService.Channels.Sum(c => c.CostPerMonth);
		}
	}
}