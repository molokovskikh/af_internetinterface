using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;

namespace InternetInterface.Models
{
	public enum ClientType
	{
		Phisical = 1,
		Legal = 2
	}

	[ActiveRecord("Clients", Schema = "Internet", Lazy = true)]
	public class Clients : ChildActiveRecordLinqBase<Clients>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual bool Disabled { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual ClientType Type { get; set; }

		[BelongsTo("PhisicalClient", Lazy = FetchWhen.OnInvoke)]
		public virtual PhisicalClients PhisicalClient { get; set; }

		[Property]
		public virtual DateTime RatedPeriodDate { get; set; }

		/*[Property]
		public virtual DateTime PreRatedPeriodDate { get; set; }*/

		[Property]
		public virtual int DebtDays { get; set; }

		[Property]
		public virtual bool FirstLease { get; set; }

		[Property]
		public virtual bool ShowBalanceWarningPage { get; set; }


		public virtual decimal GetInterval()
		{
			return (RatedPeriodDate.AddMonths(1) - RatedPeriodDate).Days + DebtDays;
		}

	}
}