using System.Collections.Generic;
using System.Linq;
using Castle.ActiveRecord;
using Common.Web.Ui.ActiveRecordExtentions;
using NHibernate;
using NHibernate.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("inforoom2_PlanChangerData", Schema = "internet")]
	public class PlanChangerData
	{ 
		[PrimaryKey]
		public virtual int Id { get; set; }

		[Property]
		public virtual int TargetPlan { get; set; }

		[Property]
		public virtual int CheapPlan { get; set; }

		[Property]
		public virtual int FastPlan { get; set; }

		[Property]
		public virtual int? NotifyDays { get; set; }

		[Property]
		public virtual int Timeout { get; set; } 
	}
}