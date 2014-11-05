using System.Collections.Generic;
using System.Web.UI.WebControls;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "Plan", NameType = typeof(Plan))]
	public class Plan : BaseModel
	{
		[Property(NotNull = true, Unique = true), NotEmpty]
		public virtual string Name { get; set; }

		[Property(NotNull = true), Min(1), NotNull]
		public virtual int Speed { get; set; }

		[Property(NotNull = true), Min(1)]
		public virtual decimal Price { get; set; }

		[Property]
		public virtual bool IsServicePlan { get; set; }

		[Property]
		public virtual bool IsArchived { get; set; }
		
		[Bag(0, Table = "region_plan")]
		[Key(1, Column = "Plan", NotNull = false)]
		[ManyToMany(2, Column = "Region", ClassType = typeof(Region))]
		public virtual IList<Region> Regions { get; set; }

		
	}
}