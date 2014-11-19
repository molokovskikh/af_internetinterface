using System.Collections;
using System.Collections.Generic;
using System.Web.Mvc;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "region", NameType = typeof(Region))]
	public class Region : BaseModel
	{
		
		[Property]
		public virtual string Name { get; set; }

		[ManyToOne(Column = "City", Cascade = "save-update")]
		public virtual City City { get; set; }
		
		[Bag(0, Table = "region_plan")]
		[Key(1, Column = "region", NotNull = false)]
		[ManyToMany(2, Column = "Plan",ClassType = typeof(Plan))]
		public virtual IList<Plan> Plans { get; set; }
		
	}
}