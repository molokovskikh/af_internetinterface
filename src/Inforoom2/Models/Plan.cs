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
	/// <summary>
	/// Тарифный план
	/// </summary>
	[Class(0, Table = "Tariffs", NameType = typeof(Plan))]
	public class Plan : BaseModel
	{
		[Property(NotNull = true, Unique = true), NotEmpty]
		public virtual string Name { get; set; }

		[Property(NotNull = true, Column = "_Speed")]
		public virtual int Speed { get; set; }

		[Property(Column = "Price",NotNull = true), Min(1)]
		public virtual decimal Price { get; set; }

		[Property]
		public virtual int FinalPriceInterval { get; set; }

		[Property]
		public virtual decimal FinalPrice { get; set; }

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

		[Bag(0, Table = "RegionPlan", Cascade = "save-update")]
		[Key(1, Column = "Plan")]
		[OneToMany(2, ClassType = typeof(RegionPlan))]
		public virtual IList<RegionPlan> RegionPlans { get; set; }

		[Property]
		public virtual int PackageId { get; set; }

		[Property]
		public virtual bool Hidden { get; set; }

		public virtual decimal SwitchPrice { get; set; }

		/// <summary>
		/// Получение стоимости перехода на другой тариф
		/// </summary>
		/// <param name="planTo">Тарифный план</param>
		/// <returns>Стоимость перехода</returns>
		public virtual decimal GetTransferPrice(Plan planTo)
		{
			var transfer = PlanTransfers.ToList().FirstOrDefault(i => i.PlanTo.Unproxy() == planTo);
			var ret = transfer != null ? transfer.Price : 0;
			return ret;
		}
	}
}