using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using Castle.Components.Validator;

namespace InternetInterface.Models
{

	[ActiveRecord("PaymentsPhisicalClient", Schema = "internet", Lazy = true)]
	public class Payment : ActiveRecordLinqBase<Payment>
	{

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime PaymentDate { get; set; }

		[Property]
		public virtual uint ClientID { get; set; }

		[Property, ValidateDecimal("Непрвильно введено значение суммы")]
		public virtual decimal Summ { get; set; }

		[BelongsTo("ManagerID")]
		public virtual Partner ManagerID { get; set; }

	}
}