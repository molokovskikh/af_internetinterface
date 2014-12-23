using System;
using System.ComponentModel;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "Payments", NameType = typeof (Payment))]
	public class Payment : BaseModel
	{
		[Property]
		public virtual DateTime RecievedOn { get; set; }

		[Property]
		public virtual DateTime PaidOn { get; set; }

		[ManyToOne(Column = "Client")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual decimal Sum { get; set; }

		[Property]
		public virtual bool BillingAccount { get; set; }

		[Property, Description("Характеризует платеж как бонусный/виртуальный")]
		public virtual bool Virtual { get; set; }

		[Property]
		public virtual string Comment { get; set; }
	}
}