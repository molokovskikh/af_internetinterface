using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Объект стоимости перехода между тарифными планами.
	/// Задает стоимость перехода с тарифа на тариф для пользователя
	/// </summary>
	[Class(0, Table = "PlanTransfer", NameType = typeof(PlanTransfer))]
	public class PlanTransfer : BaseModel
	{
		/// <summary>
		/// Тарифный план с которого переходим
		/// </summary>
		[ManyToOne(Column = "PlanFrom", Cascade = "save-update", NotNull = true)]
		public virtual Plan PlanFrom { get; set; }

		/// <summary>
		/// Тфрифный план на который переходим
		/// </summary>
		[ManyToOne(Column = "PlanTo", Cascade = "save-update", NotNull = true)]
		public virtual Plan PlanTo { get; set; }

		/// <summary>
		/// Цена перехода с одного тарифного плана на другой
		/// </summary>
		[Property(NotNull = true)]
		public virtual decimal Price { get; set; }

		[Property]
		public virtual bool IsAvailableToSwitch { get; set; }
	
	}
}