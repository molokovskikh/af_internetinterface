using System;
using System.ComponentModel;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Пулы для регионов
	/// </summary>
	[Class(0, Table = "IpPoolRegions", NameType = typeof (IpPoolRegion))]
	public class IpPoolRegion : BaseModel
	{
		public IpPoolRegion()
		{
			IpPool = new IpPool();
		}

		public IpPoolRegion(IpPool pool, Region region)
		{
			IpPool = pool;
			Region = region;
		}

		[ManyToOne(Column = "IpPool"), NotNull]
		public virtual IpPool IpPool { get; set; }

		[ManyToOne(Column = "Region", Cascade = "save-update"), NotNull]
		public virtual Region Region { get; set; }

		[Property]
		public virtual string Description { get; set; }
	}
}