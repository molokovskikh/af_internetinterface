using System.ComponentModel;
using System.Configuration;
using System.Web.Mvc;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Для акций.После истечения определенного акционного времени
	/// действие текущего целевой ТП заканчивается 
	/// и клиенту предлагается выбрать ТП из двух предложенных 
	/// </summary>
	[Class(0, Table = "PlanChangerData", NameType = typeof(PlanChangerData))]
	public class PlanChangerData : BaseModel
	{
		[ManyToOne(Column = "TargetPlan"), NotNull(Message = "выберите тариф")]
		public virtual Plan TargetPlan { get; set; }
		[ManyToOne(Column = "CheapPlan"), NotNull(Message = "выберите тариф")]
		public virtual Plan CheapPlan { get; set; }
		[ManyToOne(Column = "FastPlan"), NotNull(Message = "выберите тариф")]
		public virtual Plan FastPlan { get; set; }
		[Property, IntegerValidator, Description("Время действия целевого ТП")]
		public virtual int Timeout { get; set; }
		[Property, Description("Текст, который выводится клиенту по истечению времени действия целевого ТП")]
		public virtual string Text { get; set; } 
	}
}