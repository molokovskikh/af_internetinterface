using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "PlanTransfer", NameType = typeof(PlanTransfer))]
	public class PlanTransfer : BaseModel
	{
		[ManyToOne(Column = "PlanFrom", Cascade = "save-update", NotNull = true)]
		public virtual Plan PlanFrom { get; set; }

		[ManyToOne(Column = "PlanTo", Cascade = "save-update", NotNull = true)]
		public virtual Plan PlanTo { get; set; }

		[Property(NotNull = true)]
		public virtual decimal Price { get; set; }

		[Property]
		public virtual bool IsAvailableToSwitch { get; set; }
	
	}
}