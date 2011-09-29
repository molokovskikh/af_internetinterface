using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Castle.MonoRail.Framework.Helpers;

namespace InforoomInternet.Models
{
	[ActiveRecord(Schema = "Internet", Table = "MenuField", Lazy = true)]
	public class MenuField : ActiveRecordLinqBase<MenuField>
	{

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string Link { get; set; }

		[HasMany]
		public virtual IList<SubMenuField> subMenu { get; set; }
	}
}