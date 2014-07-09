using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Common.Web.Ui.ActiveRecordExtentions;
using InternetInterface.Models.Universal;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("NetworkZones", Schema = "internet", Lazy = true)]
	public class Zone
	{
		protected Zone()
		{
		}

		public Zone(string name, RegionHouse region = null)
		{
			Name = name;
			Region = region;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual bool IsSelfRegistrationEnabled { get; set; }

		[BelongsTo("RegionId")]
		public virtual RegionHouse Region { get; set; }

		public static IList<Zone> All()
		{
			return ArHelper.WithSession(s => s.Query<Zone>().OrderBy(z => z.Name).ToList());
		}

		public override string ToString()
		{
			return Name;
		}
	}
}