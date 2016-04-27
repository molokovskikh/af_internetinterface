using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.XPath;
using Common.Tools;
using Inforoom2.validators;
using NHibernate;
using NHibernate.Hql.Ast;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель агента
	/// </summary>
	[Class(0, Table = "BankPayments", NameType = typeof (BankPayment))]
	public class BankPayment : BaseModel
	{
		//информация ниже получается из выписки
		//фактическая дата платежа когда он прошел через банк
		[Property, ValidatorNotEmpty, Description("Дата платежа")]
		public virtual DateTime PayedOn { get; set; }

		[Property, ValidatorNumberic(0, ValidatorNumberic.Type.Greater), Description("Сумма")]
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

		public virtual bool UpdatePayerInn { get; set; }

		public virtual void RegisterPayment()
		{
			RegistredOn = SystemTime.Now();
		}

		public virtual void UpdateInn()
		{
			if (!UpdatePayerInn)
				return;

			if (Payer != null
			    && Payer.LegalClient != null
			    && Payer != null
			    && !String.IsNullOrEmpty(RecipientInn)) {
				Payer.LegalClient.Inn = PayerInn;
			}
		}


		//public string GetWarning()
		//{
		//	if (Payer != null
		//	    || PayerClient == null)
		//		return "";

		//	var payers = GetPayerForInn(PayerClient.Inn);
		//	if (payers.Count == 0) {
		//		return String.Format("Не удалось найти ни одного плательщика с ИНН {0}", PayerClient.Inn);
		//	}
		//	else if (payers.Count == 1) {
		//		Payer = payers.Single();
		//		return "";
		//	}
		//	else {
		//		return String.Format("Найдено более одного плательщика с ИНН {0}, плательщики с таким ИНН {1}",
		//			PayerClient.Inn,
		//			payers.Implode(p => p.Name));
		//	}
		//}

		//public virtual List<Client> GetPayerForInn(string INN)
		//{
		//	return ActiveRecordLinq.AsQueryable<Client>().Where(p => p.LawyerPerson.INN == INN).ToList();
		//}

	}
}