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

		[Property]
		public virtual string Description { get; set; }

		public virtual string GetNormalizeSpeed()
		{
			return GetNormalizeSpeed(Speed);
		}

		public override string ToString()
		{
			return GetNormalizeSpeed();
		}

		public static string GetNormalizeSpeed(int? speed)
		{
			if (!speed.HasValue)
				return "";
			return GetNormalizeSpeed(speed.Value);
		}

		public static string GetNormalizeSpeed(int speed)
		{
			float mb = speed / 1000000.00f;
			return mb >= 1 ? mb.ToString("0.00") + " мб/с" : (mb * 1000).ToString("0.00") + " кб/с";
		}
	}
}