using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord("BypassHouses", Schema = "internet", Lazy = true)]
	public class BypassHouse : ActiveRecordLinqBase<BypassHouse>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual House House { get; set; }

		[BelongsTo]
		public virtual Partner Agent { get; set; }

		[Property]
		public virtual DateTime BypassDate { get; set; }
	}
}