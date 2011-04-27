using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord("PackageSpeed", Schema = "internet", Lazy = true)]
	public class PackageSpeed : ActiveRecordLinqBase<PackageSpeed>
	{
		[PrimaryKey]
		public virtual int Id { get; set; }

		[Property]
		public virtual int PackageId { get; set; }

		[Property]
		public virtual int Speed { get; set; }
	}
}