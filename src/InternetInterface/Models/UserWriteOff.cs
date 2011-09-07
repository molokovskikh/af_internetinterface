using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord("UserWriteOffs", Schema = "internet", Lazy = true)]
	public class UserWriteOff : ActiveRecordLinqBase<UserWriteOff>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual decimal Sum { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[BelongsTo("Client")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual bool BillingAccount { get; set; }

		/*public virtual string GetDate(string grouped)
		{
			if (grouped == "month")
				return string.Format("{0}.{1}", Date.Month.ToString("00"), Date.Year);
			if (grouped == "year")
				return string.Format("{0}", Date.Year);
			return string.Format("{0}.{1}.{2}", Date.Day.ToString("00"), Date.Month.ToString("00"), Date.Year);
		}*/
	}
}