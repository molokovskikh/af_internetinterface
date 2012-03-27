using System;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{

	[ActiveRecord("Payments", Schema = "internet", Lazy = true)]
	public class Payment : ValidActiveRecordLinqBase<Payment>
	{

		/*public Payment()
		{
			Client.Payments.Add(this);
		}*/

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime RecievedOn { get; set; }

		[Property]
		public virtual DateTime PaidOn { get; set; }

		[BelongsTo("Client")]
		public virtual Client Client { get; set; }

		[Property, ValidateNonEmpty("Введите сумму"),
				   ValidateRange(1, 100000, "Сумма должна быть больше 0 и меньше 100 000 рублей"),
				   ValidateDecimal("Некорректно введено значение суммы")]
		public virtual decimal Sum { get; set; }

		[BelongsTo("Agent")]
		public virtual Agent Agent { get; set; }

		[Property]
		public virtual bool BillingAccount { get; set; }

		[Property]
		public virtual bool Virtual { get; set; }
	}
}