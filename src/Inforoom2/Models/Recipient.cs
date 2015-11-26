using System.ComponentModel;
using System.Globalization;
using System.Web.Services.Description;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель города
	/// </summary>
	[Class(0, Table = "Recipients", NameType = typeof (Recipient), Schema = "Billing")]
	public class Recipient : BaseModel
	{
		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string FullName { get; set; }

		[Property]
		public virtual string Address { get; set; }

		[Property]
		public virtual string INN { get; set; }

		[Property]
		public virtual string KPP { get; set; }

		[Property]
		public virtual string BIC { get; set; }

		[Property]
		public virtual string Bank { get; set; }

		[Property]
		public virtual string BankLoroAccount { get; set; }

		[Property]
		public virtual string BankAccountNumber { get; set; }

		[Property]
		public virtual string Boss { get; set; }

		[Property]
		public virtual string Accountant { get; set; }

		[Property]
		public virtual string AccountWarranty { get; set; }
	}
}