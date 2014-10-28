using System.Collections.Generic;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "Plan", NameType = typeof(Plan))]
	public class Plan : BaseModel
	{
		[Property]
		public virtual string Name { get; set; }

		[Bag(0, Table = "region_plan")]
		[Key(1, Column = "Plan", NotNull = false)]
		[ManyToMany(2, Column = "Region", ClassType = typeof(Region))]
		public virtual IList<Region> Regions { get; set; }

		public virtual bool Checked { get; set; }
	}
}