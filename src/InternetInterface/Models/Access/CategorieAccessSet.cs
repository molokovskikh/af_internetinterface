using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
	[ActiveRecord("CategoriesAccessSet", Schema = "Internet", Lazy = true)]
	public class CategorieAccessSet : ActiveRecordLinqBase<CategorieAccessSet>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("Categorie")]
		public virtual UserRole Categorie { get; set; }

		[BelongsTo("AccessCat")]
		public virtual AccessCategories AccessCat { get; set; }
	}
}