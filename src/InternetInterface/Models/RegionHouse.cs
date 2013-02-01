using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord("Regions", Schema = "internet", Lazy = true)]
	public class RegionHouse : ChildActiveRecordLinqBase<RegionHouse>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property("Region")]
		public virtual string Name { get; set; }

		public static IList<RegionHouse> All()
		{
			return Queryable
				.OrderBy(r => r.Name)
				.ToList();
		}
	}
}