using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.Components.Validator;
using Common.Web.Ui.Helpers;
using InternetInterface.Controllers.Filter;

namespace InternetInterface.Models
{
	[ActiveRecord("Bookmarks", Schema = "internet", Lazy = true)]
	public class Bookmark
	{
		public Bookmark()
		{
			Creator = InitializeContent.Partner;
			CreateDate = DateTime.Now;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property, ValidateNonEmpty("Введите такст закладки")]
		public virtual string Text { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[BelongsTo]
		public virtual Partner Creator { get; set; }

		[Property]
		public virtual DateTime CreateDate { get; set; }

		[Property, Style]
		public virtual bool Deleted { get; set; }

		public virtual string GetTransformed()
		{
			return AppealHelper.GetTransformedAppeal(Text);
		}
	}
}