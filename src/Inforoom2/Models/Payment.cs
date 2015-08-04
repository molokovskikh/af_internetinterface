using System;
using System.ComponentModel;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель платежей
	/// </summary>
	[Class(0, Table = "Payments", NameType = typeof(Payment))]
	public class Payment : BaseModel
	{
		[Property, Description("Дата получения платежа")]
		public virtual DateTime RecievedOn { get; set; }

		[Property, Description("Дата оплаты платежа")]
		public virtual DateTime PaidOn { get; set; }

		[Property, Description("Сумма платежа")]
		public virtual decimal Sum { get; set; }

		[ManyToOne(Column = "Client")]
		public virtual Client Client { get; set; }

		[ManyToOne(Column = "Agent"), NotNull]
		public virtual Employee Employee { get; set; }

		[Property, Description("Отметка о том,что обработано биллингом ")]
		public virtual bool BillingAccount { get; set; }

		[Property, Description("Характеризует платеж как бонусный/виртуальный")]
		public virtual bool? Virtual { get; set; }

		[Property, Description("Комментарий к платежу")]
		public virtual string Comment { get; set; }
	}
}