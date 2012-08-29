using System.Linq;
using Castle.ActiveRecord;
using InternetInterface.Services;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "IpTvBoxRent")]
	public class IpTvBoxRent : Service
	{
		public IpTvBoxRent(decimal price)
		{
			Price = price;
		}

		public IpTvBoxRent()
		{
		}

		public override bool ProcessEvenInBlock
		{
			get { return true; }
		}

		public override decimal GetPrice(ClientService assignedService)
		{
			if (assignedService.Client.Status.Type == StatusType.Dissolved)
				return 0;
			if (IsRentForFree(assignedService))
				return 0;
			return base.GetPrice(assignedService);
		}

		public override void WriteOff(ClientService assignedService)
		{
			if (!IsRentForFree(assignedService))
				return;

			var writeOff = assignedService.Client.CalculatePerDayWriteOff(base.GetPrice(assignedService), false);
			if (writeOff == null)
				return;
			var client = assignedService.Client.PhysicalClient;
			client.MoneyBalance -= writeOff.MoneySum;
			client.VirtualBalance += writeOff.MoneySum;
		}

		private static bool IsRentForFree(ClientService assignedService)
		{
			var client = assignedService.Client;
			var blocking = client.FindService<VoluntaryBlockin>();
			var blockedForFree = blocking != null && blocking.GetPrice() == 0;
			if (blockedForFree)
				return true;
			if (blocking == null && client.HaveService<IpTv>())
				return true;
			return false;
		}
	}
}