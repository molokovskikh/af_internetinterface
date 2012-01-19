using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord("SmsMessages", Schema = "internet", Lazy = true)]
	public class SmsMessage : ActiveRecordLinqBase<SmsMessage>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual DateTime CreateDate { get; set; }

		[Property]
		public virtual DateTime? SendToOperatorDate { get; set; }

		[Property]
		public virtual DateTime? ShouldBeSend { get; set; }

		[Property]
		public virtual string Text { get; set; }

		[Property]
		public virtual string PhoneNumber { get; set; }

		[BelongsTo]
		public virtual Client Client { get; set; }

		[Property]
		public virtual bool IsSended { get; set; }

		[Property]
		public virtual string SMSID { get; set; }
	}
}