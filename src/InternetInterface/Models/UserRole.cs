using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	[ActiveRecord("UserCategories", Schema = "Internet", Lazy = true)]
	public class UserRole : ChildActiveRecordLinqBase<UserRole>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[Property]
		public virtual string ReductionName { get; set; }

		public virtual bool HasExAttrs
		{
			get { return ReductionName != "Office"; }
		}
	}
}