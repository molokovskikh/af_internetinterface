using System;
using System.ComponentModel;
using System.Configuration;
using System.Web.Mvc;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// История смены тарифных планов
	/// </summary>
	[Class(0, Table = "plan_history", NameType = typeof(PlanHistoryEntry))]
	public class PlanHistoryEntry : BaseModel
	{
		[ManyToOne, NotNull(Message = "укажите клиента"), Description("Клиент")]
		public virtual Client Client { get; set; }

		[ManyToOne, NotNull(Message = "укажите тариф"), Description("Тариф до перехода")]
		public virtual Plan PlanBefore { get; set; }

		[ManyToOne, NotNull(Message = "укажите тариф"), Description("Тариф после перехода")]
		public virtual Plan PlanAfter { get; set; }

		[Property, Description("Время перехода")]
		public virtual DateTime DateOfChange { get; set; }

		[Property, Description("Стоимость перехода")]
		public virtual decimal Price { get; set; } 
	}
}