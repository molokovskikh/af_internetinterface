﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
	[ActiveRecord("PaymentForConnect", Schema = "internet", Lazy = true)]
	public class PaymentForConnect : ChildActiveRecordLinqBase<PaymentForConnect>
	{
		public PaymentForConnect()
		{
		}

		public PaymentForConnect(decimal sum, ClientEndpoint point)
		{
			Sum = sum;
			EndPoint = point;
			RegDate = DateTime.Now;
			Partner = InitializeContent.TryGetPartner();
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime? RegDate { get; set; }

		[Property]
		public virtual decimal Sum { get; set; }

		[BelongsTo]
		public virtual Partner Partner { get; set; }

		[BelongsTo]
		public virtual ClientEndpoint EndPoint { get; set; }

		[Property]
		public virtual bool Paid { get; set; }
	}
}