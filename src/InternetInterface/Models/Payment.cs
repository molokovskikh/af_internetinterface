using System;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Web.Ui.Helpers;
using InternetInterface.Models.Universal;

namespace InternetInterface.Models
{
	[ActiveRecord("Payments", Schema = "internet", Lazy = true)]
	public class Payment : ValidActiveRecordLinqBase<Payment>
	{
		public Payment()
		{
		}

		public Payment(Client client, decimal sum)
		{
			PaidOn = DateTime.Now;
			Client = client;
			Sum = sum;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime RecievedOn { get; set; }

		[Property]
		public virtual DateTime PaidOn { get; set; }

		[BelongsTo("Client")]
		public virtual Client Client { get; set; }

		[Property,
		 ValidateNonEmpty("Введите сумму"),
		 ValidateRange(1, 100000, "Сумма должна быть больше 0 и меньше 100 000 рублей"),
		 ValidateDecimal("Некорректно введено значение суммы")]
		public virtual decimal Sum { get; set; }

		[BelongsTo("Agent")]
		public virtual Agent Agent { get; set; }

		[Property]
		public virtual bool BillingAccount { get; set; }

		[Property, Style]
		public virtual bool Virtual { get; set; }

		[BelongsTo]
		public virtual BankPayment BankPayment { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[Style]
		public virtual bool NotProcessed
		{
			get { return !BillingAccount; }
		}

		public virtual Appeals Cancel(string comment)
		{
			if (BillingAccount) {
				if (Client.PhysicalClient != null)
					Client.PhysicalClient.WriteOff(Sum, Virtual);
				else
					Client.LawyerPerson.Balance -= Sum;
			}
			return Appeals.CreareAppeal(String.Format("Удален платеж на сумму {0:C} \r\n Комментарий: {1}", Sum, comment), Client, AppealType.System);
		}
	}
}