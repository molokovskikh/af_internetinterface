using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using InternetInterface.Services;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(DiscriminatorValue = "IpTv")]
	public class IpTv : Service
	{
		public IpTv()
		{
			Channels = new List<ChannelGroup>();
		}

		public IpTv(params ChannelGroup[] channels)
			: this()
		{
			Channels = channels.ToList();
		}

		[HasAndBelongsToMany(
			Schema = "Internet",
			Table = "AssignedChannels",
			ColumnKey = "AssignedServiceId",
			ColumnRef = "ChannelGroupId")]
		public virtual IList<ChannelGroup> Channels { get; set; }

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
				return Channels.Sum(c => c.CostPerMonthWithInternet);

			return Channels.Sum(c => c.CostPerMonth);
		}
	}
}