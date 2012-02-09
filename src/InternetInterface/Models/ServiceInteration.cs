using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "internet", Lazy = true)]
	public class ServiceInteration
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Description { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[BelongsTo]
		public virtual ServiceRequest Request { get; set; }
	}
}