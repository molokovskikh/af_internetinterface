using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
			return InitializeContent.Partner.AccesedPartner.Contains(reduseRulesName);
		}

		public static IList<CategorieAccessSet> GetAccessPartner()
		{
			return FindAll(DetachedCriteria.For(typeof(CategorieAccessSet))
				.Add(Restrictions.Eq("Categorie", InitializeContent.Partner.Categorie)));
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