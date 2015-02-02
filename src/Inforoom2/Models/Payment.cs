using System;
using System.ComponentModel;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "Payments", NameType = typeof (Payment))]
	public class Payment : BaseModel
	{
		[Property]
		public virtual DateTime RecievedOn { get; set; }

		[Property]
		public virtual DateTime PaidOn { get; set; }

		[Property]
		public virtual decimal Sum { get; set; }

		[ManyToOne(Column = "Client")]
		public virtual Client Client { get; set; }

		[ManyToOne(Column = "Agent"), NotNull]
		public virtual Employee Employee { get; set; }

		[Property]
		public virtual bool BillingAccount { get; set; }

		[Property, Description("Характеризует платеж как бонусный/виртуальный")]
		public virtual bool? Virtual { get; set; }

		[Property]
		public virtual string Comment { get; set; }
	}
}