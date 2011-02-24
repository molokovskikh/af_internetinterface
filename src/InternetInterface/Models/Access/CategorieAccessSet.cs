using System;
using System.Collections.Generic;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.ActiveRecord.Linq;
using InternetInterface.Controllers.Filter;
using NHibernate.Criterion;
using NHibernate.SqlCommand;

namespace InternetInterface.Models
{
	[ActiveRecord("CategoriesAccessSet", Schema = "Internet", Lazy = true)]
	public class CategorieAccessSet : ActiveRecordLinqBase<CategorieAccessSet>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("Categorie")]
		public virtual UserCategorie Categorie { get; set; }

		[BelongsTo("AccessCat")]
		public virtual AccessCategories AccessCat { get; set; }

		public static Boolean AccesPartner(string reduseRulesName)
		{
			var result = FindAll(DetachedCriteria.For(typeof(CategorieAccessSet))
									.CreateAlias("AccessCat", "AC", JoinType.InnerJoin)
									.Add(Restrictions.Eq("Categorie", InithializeContent.partner.Categorie))
									.Add(Restrictions.Eq("AC.ReduceName", reduseRulesName)));
			return result.Length != 0 ? true : false;
		}

		public static IList<CategorieAccessSet> GetAccessPartner()
		{
			return FindAll(DetachedCriteria.For(typeof(CategorieAccessSet))
									.Add(Restrictions.Eq("Categorie", InithializeContent.partner.Categorie)));
		}

		public override void SaveAndFlush()
		{
			base.SaveAndFlush();
			AccessCat.AcceptTo(Categorie);
		}

		public override void DeleteAndFlush()
		{
			base.DeleteAndFlush();
			AccessCat.DeleteTo(Categorie);
		}
	}

}