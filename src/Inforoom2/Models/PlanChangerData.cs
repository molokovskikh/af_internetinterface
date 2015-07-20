using System.Configuration;
using System.Web.Mvc;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "PlanChangerData", NameType = typeof(PlanChangerData))]
	public class PlanChangerData : BaseModel
	{
		[ManyToOne(Column = "TargetPlan"), NotNull(Message = "выберите тариф")]
		public virtual Plan TargetPlan { get; set; }
		[ManyToOne(Column = "CheapPlan"), NotNull(Message = "выберите тариф")]
		public virtual Plan CheapPlan { get; set; }
		[ManyToOne(Column = "FastPlan"), NotNull(Message = "выберите тариф")]
		public virtual Plan FastPlan { get; set; }
		[Property, IntegerValidator]
		public virtual int Timeout { get; set; }
		[Property]
		public virtual string Text { get; set; } 
	}
}