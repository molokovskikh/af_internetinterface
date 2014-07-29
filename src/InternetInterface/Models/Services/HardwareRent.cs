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
			if (IsRentForFree(assignedService))
				return 0;
			return assignedService.RentableHardware.Cost;
		}

		public override void WriteOff(ClientService assignedService)
		{
			if (!IsRentForFree(assignedService))
				return;

			var writeOff = assignedService.Client.CalculatePerDayWriteOff(assignedService.RentableHardware.Cost);
			if (writeOff == null)
				return;
			var client = assignedService.Client.PhysicalClient;
			client.MoneyBalance -= writeOff.MoneySum;
			client.VirtualBalance += writeOff.MoneySum;
		}

		private static bool IsRentForFree(ClientService assignedService)
		{
			var client = assignedService.Client;
			var blocking = client.FindActiveService<VoluntaryBlockin>();
			var blockedForFree = blocking != null && blocking.GetPrice() == 0;
			if (blockedForFree)
				return true;
			if (blocking == null && client.Balance > 0)
				return true;
			return false;
		}

		public override bool CanActivateInWeb(Client client)
		{
			return true;
		}
	}
}