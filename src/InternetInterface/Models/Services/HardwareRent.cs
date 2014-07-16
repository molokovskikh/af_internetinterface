using Castle.ActiveRecord;
using InternetInterface.Services;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "HardwareRent")]
	public class HardwareRent : Service
	{
		public override bool ProcessEvenInBlock
		{
			get { return true; }
		}

		public override decimal GetPrice(ClientService assignedService)
		{
			if (assignedService.Client.Status.Type == StatusType.Dissolved)
				return 0;
			return base.GetPrice(assignedService);
		}
	}
}