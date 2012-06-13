using Castle.ActiveRecord;

namespace InternetInterface.Models.Services
{
	[ActiveRecord(Schema = "Internet")]
	public class IpTvChannelGroup
	{
		public IpTvChannelGroup()
		{}

		public IpTvChannelGroup(string name,
			decimal costPerMonth,
			decimal costPerMonthWithInternet)
		{
			Name = name;
			CostPerMonth = costPerMonth;
			CostPerMonthWithInternet = costPerMonthWithInternet;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual decimal CostPerMonth { get; set; }

		[Property]
		public virtual decimal CostPerMonthWithInternet { get; set; }
	}
}