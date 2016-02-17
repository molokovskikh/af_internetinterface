using System;
using System.ComponentModel;
using Common.Tools;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель платежей
	/// </summary>
	[Class(0, Table = "Payments", NameType = typeof (Payment))]
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

		[ManyToOne(Column = "BankPayment")]
		public virtual BankPayment BankPayment { get; set; }

		[ManyToOne(Column = "Agent")]
		public virtual Employee Employee { get; set; }

		[Property, Description("Отметка о том,что обработано биллингом ")]
		public virtual bool BillingAccount { get; set; }

		[Property, Description("Характеризует платеж как бонусный/виртуальный")]
		public virtual bool? Virtual { get; set; }

		[Property, Description("Комментарий к платежу")]
		public virtual string Comment { get; set; }

		[Property, Description("Платеж является дубликатом")]
		public virtual bool IsDuplicate { get; set; }

		public virtual string SumToLiteral()
		{
			return TextUtil.NumToPaymentString((double)Sum);
		}

		public virtual Appeal Cancel(string comment, Employee employee)
		{
			if (BillingAccount)
				Client.WriteOff(Sum, Virtual.HasValue && Virtual.Value);

			return new Appeal(String.Format("Удален платеж на сумму {0} \r\n Комментарий: {1}", Sum.ToString("0.00"), comment), Client,
				AppealType.System) {Employee = employee};
		}
	}
}