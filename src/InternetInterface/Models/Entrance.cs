using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord("Entrances", Schema = "internet", Lazy = true)]
	public class Entrance : ActiveRecordLinqBase<Entrance>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual House House { get; set; }

		[Property]
		public virtual int Number { get; set; }

		[Property]
		public virtual bool Strut { get; set; }

		[Property]
		public virtual bool Cable { get; set; }

		[BelongsTo]
		public virtual NetworkSwitches Switch { get; set; }

		public virtual bool WasConnected()
		{
			return Switch != null;
		}
	}
}