using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using InternetInterface.Controllers.Filter;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
namespace InternetInterface.Models
{
	[ActiveRecord("PartnerAccessSet", Schema = "Internet", Lazy = true)]
	public class PartnerAccessSet : ActiveRecordLinqBase<PartnerAccessSet>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("Partner")]
		public virtual Partner PartnerId { get; set; }

		[BelongsTo("AccessCat")]
		public virtual AccessCategories AccessCat { get; set; }

		public static Boolean AccesPartner(string reduseRulesName)
		{
			var result = FindAll(DetachedCriteria.For(typeof (PartnerAccessSet))
									.CreateAlias("AccessCat", "AC", JoinType.InnerJoin)
									.Add(Restrictions.Eq("PartnerId", InithializeContent.partner))
									.Add(Restrictions.Eq("AC.ReduceName", reduseRulesName)));
			return result.Length != 0 ? true : false;
		}

		public static IList<PartnerAccessSet> GetAccessPartner()
		{
			return FindAll(DetachedCriteria.For(typeof(PartnerAccessSet))
									.Add(Restrictions.Eq("PartnerId", InithializeContent.partner)));
		}
	}

}