using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Web.Ui.Models.Audit;
using Common.Web.Ui.MonoRailExtentions;

namespace InternetInterface.Models
{
	/// <summary>
	/// Услуга
	/// </summary>
	[ActiveRecord("OrderServices", Schema = "Internet", Lazy = true), Auditable]
	public class OrderService
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, Description("Описание")]
		public virtual string Description { get; set; }

		[Property, Description("Стоимость"), Auditable("Стоимость услуги"), ValidateDecimal("Ошибка ввода суммы"), ValidateGreaterThanZero]
		public virtual decimal Cost { get; set; }

		[Property, Description("Услуга периодичная")]
		public virtual bool IsPeriodic { get; set; }

		[BelongsTo(Column = "OrderId")]
		public virtual Order Order { get; set; }

		public virtual string GetPeriodic()
		{
			if (IsPeriodic) {
				return "Периодичная";
			}
			return "Разовая";
		}
	}
}