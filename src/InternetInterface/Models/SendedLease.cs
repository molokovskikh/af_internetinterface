using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	[ActiveRecord("SendedLeases", Schema = "Internet", Lazy = true)]
	public class SendedLease: ChildActiveRecordLinqBase<SendedLease>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }
	
		[Property]
		public virtual uint LeaseId { get; set; }

		[Property]
		public virtual string LeasedTo { get; set; }

		[Property]
		public virtual DateTime LeaseBegin { get; set; }
	
		[Property]
		public virtual DateTime LeaseEnd { get; set; }

		[Property]
		public virtual uint Ip { get; set; }

		[Property]
		public virtual uint? Pool { get; set; }

		[Property]
		public virtual int Module { get; set; }

		[Property]
		public virtual int? Port { get; set; }

		[BelongsTo("Switch")]
		public virtual NetworkSwitches Switch { get; set; }

		[Property]
		public virtual DateTime SendDate { get; set; }

		[BelongsTo]
		public virtual ClientEndpoints Endpoint { get; set; }
	}
}