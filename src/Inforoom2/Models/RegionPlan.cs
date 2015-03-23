using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "RegionPlan", NameType = typeof(RegionPlan))]
	public class RegionPlan : BaseModel
	{
		[ManyToOne(Cascade = "save-update", NotNull = true)]
		public virtual Plan Plan { get; set; }

		[ManyToOne(Cascade = "save-update", NotNull = true)]
		public virtual Region Region { get; set; }

	}


}
