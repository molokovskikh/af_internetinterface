using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InforoomInternet.Models
{
	[ActiveRecord(Schema = "Internet", Table = "SubMenuField", Lazy = true)]
	public class SubMenuField : ActiveRecordLinqBase<SubMenuField>
	{

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual MenuField MenuField { get; set; }

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string Link { get; set; }
	}
}