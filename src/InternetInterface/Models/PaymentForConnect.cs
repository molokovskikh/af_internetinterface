using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	[ActiveRecord("PaymentForConnect", Schema = "internet", Lazy = true)]
	public class PaymentForConnect : ChildActiveRecordLinqBase<PaymentForConnect>
	{
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
	}
}