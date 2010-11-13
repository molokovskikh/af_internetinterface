using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;
using InternetInterface.Controllers.Filter;
using NHibernate.Criterion;

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

		public static Boolean AccesPartner(AccessCategoriesType accessOption)
		{
			var result = FindAll(DetachedCriteria.For(typeof (PartnerAccessSet))
									.Add(Restrictions.Eq("PartnerId", InithializeContent.partner))
									.Add(Restrictions.Eq("AccessCat.Id", (Int32)accessOption)));
			return result.Length != 0 ? true : false;
		}

		public static IList<PartnerAccessSet> GetAccessPartner()
		{
			return FindAll(DetachedCriteria.For(typeof(PartnerAccessSet))
									.Add(Restrictions.Eq("PartnerId", InithializeContent.partner)));
		}
	}

}