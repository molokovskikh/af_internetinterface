using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord("NetworkZones", Schema = "internet", Lazy = true)]
	public class WriteOff : ActiveRecordLinqBase<WriteOff>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual decimal WriteOffSum { get; set; }

		[Property]
		public virtual DateTime WriteOffDate { get; set; }

		[BelongsTo("Client")]
		public virtual Clients Client { get; set; }

	}
}