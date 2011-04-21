using System;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{

	/*[ActiveRecord("PaymentForConnect", Schema = "internet", Lazy = true)]
	public class PaymentForConnect : ValidActiveRecordLinqBase<PaymentForConnect>
	{

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime PaymentDate { get; set; }

		[BelongsTo("ClientId")]
		public virtual PhysicalClients ClientId { get; set; }

		[Property, ValidateNonEmpty("Введите сумму"), ValidateDecimal("Непрвильно введено значение суммы")]
		public virtual string Summ { get; set; }

		[BelongsTo("ManagerID")]
		public virtual Partner ManagerID { get; set; }

	}*/
}