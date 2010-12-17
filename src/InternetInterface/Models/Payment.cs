using System;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{

	[ActiveRecord("Payments", Schema = "internet", Lazy = true)]
	public class Payment : ValidActiveRecordLinqBase<Payment>
	{

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime RecievedOn { get; set; }

		[Property]
		public virtual DateTime PaidOn { get; set; }

		[BelongsTo("Client")]
		public virtual PhisicalClients Client { get; set; }

		[Property, ValidateNonEmpty("Введите сумму") , ValidateDecimal("Непрвильно введено значение суммы")]
		public virtual string Sum { get; set; }

		[BelongsTo("Agent")]
		public virtual Agent Agent { get; set; }

	}
}