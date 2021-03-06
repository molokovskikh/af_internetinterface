﻿using System;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Tools;
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

		public Payment(Client client, Payment payment)
			: this(client, payment.Sum)
		{
			RecievedOn = payment.RecievedOn;
			Virtual = payment.Virtual;
			Agent = payment.Agent;
			BankPayment = payment.BankPayment;
			Comment = payment.Comment;
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

		[BelongsTo("Partner", Column = "Agent")]
		public virtual Partner Agent { get; set; }

		[Property]
		public virtual bool BillingAccount { get; set; }

		[Property("`Virtual`"), Style]
		public virtual bool Virtual { get; set; }

		[Property]
		public virtual bool IsDuplicate { get; set; }
		
		[BelongsTo]
		public virtual BankPayment BankPayment { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[Property]
		public virtual string TransactionId { get; set; }

		[Style]
		public virtual bool NotProcessed
		{
			get { return !BillingAccount; }
		}

		public virtual Appeals Cancel(string comment)
		{
			if (BillingAccount)
				Client.WriteOff(Sum, Virtual);

			return Client.CreareAppeal(String.Format("Удален платеж на сумму {0:C} \r\n Комментарий: {1}", Sum, comment), AppealType.System);
		}

		public virtual string SumToLiteral()
		{
			return TextUtil.NumToPaymentString((double)Sum);
		}
	}
}