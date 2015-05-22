﻿using System;
using Common.Tools;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "PaymentForConnect", NameType = typeof(PaymentForConnect))]
	public class PaymentForConnect : BaseModel
	{
		[Property]
		public virtual DateTime? RegDate { get; set; }

		[Property]
		public virtual decimal Sum { get; set; }

		[ManyToOne(Column = "Partner")]
		public virtual Employee Employee { get; set; }

		[ManyToOne(Column = "Endpoint")]
		public virtual ClientEndpoint EndPoint { get; set; }

		[Property]
		public virtual bool Paid { get; set; }

		public PaymentForConnect()
		{
		}

		public PaymentForConnect(decimal sum, ClientEndpoint point)
		{
			Sum = sum;
			EndPoint = point;
			RegDate = SystemTime.Now();
			Employee = null;
		}
	}
}