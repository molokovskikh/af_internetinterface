using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{ 
	/// <summary>
	/// Модель агента
	/// </summary>
	[Class(0, Table = "BankPayments", NameType = typeof(BankPayment))]
	public class BankPayment : BaseModel
	{


		//информация ниже получается из выписки
		//фактическая дата платежа когда он прошел через банк
		[Property,  Description("Дата платежа")]
		public virtual DateTime PayedOn { get; set; }

		[Property,  Description("Сумма")]
		public virtual decimal Sum { get; set; }

		[Property, Description("Описание платежа")]
		public virtual string Comment { get; set; }

		[Property, Description("Номер документа")]
		public virtual string DocumentNumber { get; set; }

		[Property]
		public virtual string PayerInn { get; set; }
		[Property]
		public virtual string PayerName { get; set; }
		[Property]
		public virtual string PayerAccountCode { get; set; }
		[Property]
		public virtual string PayerBankDescription { get; set; }
		[Property]
		public virtual string PayerBankAccountCode { get; set; }
		[Property]
		public virtual string PayerBankBic { get; set; }
		[Property]
		public virtual string RecipientInn { get; set; }
		[Property]
		public virtual string RecipientName { get; set; }
		[Property]
		public virtual string RecipientAccountCode { get; set; }
		[Property]
		public virtual string RecipientBankDescription { get; set; }
		[Property]
		public virtual string RecipientBankBic { get; set; }
		[Property]
		public virtual string RecipientBankAccountCode { get; set; }



		//все что выше получается из выписки
		//дата занесения платежа

		[ManyToOne(Column = "PayerId")]
		public virtual Client Payer { get; set; }
		
		[ManyToOne(Column = "RecipientId")]
		public virtual Recipient Recipient { get; set; }

		[Property, Description("Когда зарегистрирован")]
		public virtual DateTime RegistredOn { get; set; }

		[Property, Description("Комментарий оператора")]
		public virtual string OperatorComment { get; set; }

		[OneToOne(PropertyRef = "BankPayment")]
		public virtual Payment Payment { get; set; }
		 
	}
}