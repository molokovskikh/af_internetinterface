using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using InternetInterface.Services;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "IpTv")]
	public class IpTv : Service
	{
		public override bool SupportUserActivation
		{
			get
			{
				return true;
			}
		}

		public override decimal GetPrice(ClientService assignedService)
		{
			if (assignedService.Client.Disabled
				|| !assignedService.ActivatedByUser)
				return 0;

			if (assignedService.Client.HaveService<Internet>())
				return assignedService.Channels.Sum(c => c.CostPerMonthWithInternet);

			return assignedService.Channels.Sum(c => c.CostPerMonth);
		}
	}
}