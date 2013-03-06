using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	/// <summary>
	/// Услуга
	/// </summary>
	[ActiveRecord("OrderServices", Schema = "Internet", Lazy = true)]
	public class OrderService
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, Description("Описание")]
		public virtual string Description { get; set; }

		[Property, Description("Стоимость")]
		public virtual decimal Cost { get; set; }

		[Property, Description("Услуга периодичная")]
		public virtual bool IsPeriodic { get; set; }

		[BelongsTo(Column = "OrderId")]
		public virtual Orders Order { get; set; }

		public virtual string GetPeriodic()
		{
			if (IsPeriodic) {
				return "Периодичная";
			}
			return "Разовая";
		}
	}
}