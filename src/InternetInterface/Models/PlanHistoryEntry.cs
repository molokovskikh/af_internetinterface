using System;
using System.ComponentModel;
using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	[ActiveRecord("inforoom2_plan_history", Schema = "Internet")]
	public class PlanHistoryEntry
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("Client", Lazy = FetchWhen.OnInvoke)]
		public virtual Client Client { get; set; }

		[BelongsTo("PlanBefore", Lazy = FetchWhen.OnInvoke)]
		public virtual Tariff PlanBefore { get; set; }

		[BelongsTo("PlanAfter", Lazy = FetchWhen.OnInvoke)]
		public virtual Tariff PlanAfter { get; set; }

		[Property, Description("Время перехода")]
		public virtual DateTime DateOfChange { get; set; }

		[Property, Description("Стоимость перехода")]
		public virtual decimal Price { get; set; }
	}
}