using System.Collections;
using System.Collections.Generic;
using System.Web.Mvc;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "Regions", NameType = typeof(Region))]
	public class Region : BaseModel
	{
		[Property(Column = "Region")]
		public virtual string Name { get; set; }

		[ManyToOne(Column = "_City",  Cascade = "save-update")]
		public virtual City City { get; set; }
		
		[Bag(0, Table = "region_plan")]
		[Key(1, Column = "region", NotNull = false)]
		[ManyToMany(2, Column = "Plan",ClassType = typeof(Plan))]
		public virtual IList<Plan> Plans { get; set; }

		[Property(Column = "_RegionOfficePhoneNumber")]
		public virtual string RegionOfficePhoneNumber { get; set; }

		[Bag(0, Table = "Street", Cascade = "all-delete-orphan")]
		[Key(1, Column = "Region")]
		[OneToMany(2, ClassType = typeof(Street))]
		public virtual IList<Street> Streets { get; set; }

	}
}