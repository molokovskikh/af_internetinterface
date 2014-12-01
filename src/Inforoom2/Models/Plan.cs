using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Inforoom2.Intefaces;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "Tariffs", NameType = typeof(Plan))]
	public class Plan : BaseModel
	{
		[Property(NotNull = true, Unique = true), NotEmpty]
		public virtual string Name { get; set; }

		[Property(NotNull = true, Column = "_Speed")]
		public virtual int Speed { get; set; }

		[Property(Column = "Price",NotNull = true), Min(1)]
		public virtual decimal Price { get; set; }

		[Property(Column = "_IsServicePlan")]
		public virtual bool IsServicePlan { get; set; }

		[Property(Column = "_IsArchived")]
		public virtual bool IsArchived { get; set; }

		[Bag(0, Table = "region_plan")]
		[Key(1, Column = "Plan", NotNull = false)]
		[ManyToMany(2, Column = "Region", ClassType = typeof(Region))]
		public virtual IList<Region> Regions { get; set; }

		[Bag(0, Table = "PlanTransfer", Cascade = "save-update")]
		[Key(1, Column = "PlanFrom")]
		[OneToMany(2, ClassType = typeof(PlanTransfer))]
		public virtual IList<PlanTransfer> PlanTransfers { get; set; }

		[Property]
		public virtual int PackageId { get; set; }
		
		public virtual decimal SwitchPrice { get; set; }

		public virtual void AddPlanTransfer(Plan plan, decimal price)
		{
			PlanTransfer planTransfer = new PlanTransfer();
			planTransfer.PlanTo = plan;
			planTransfer.PlanFrom = this;
			planTransfer.Price = price;
			PlanTransfers.Add(planTransfer);
			//plan.PlanTransfers.Add(planTransfer);
		}
	}
}