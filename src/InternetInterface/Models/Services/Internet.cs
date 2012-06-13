using Castle.ActiveRecord;
using Common.Tools;
using InternetInterface.Services;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "Internet")]
	public class Internet : Service
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
			return assignedService.Client.GetTariffPrice();
		}
	}
}