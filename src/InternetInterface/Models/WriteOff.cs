using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord("WriteOff", Schema = "internet", Lazy = true)]
	public class WriteOff : ActiveRecordLinqBase<WriteOff>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual decimal WriteOffSum { get; set; }

		[Property]
		public virtual DateTime WriteOffDate { get; set; }

		[BelongsTo("Client")]
		public virtual Clients Client { get; set; }

		public virtual string GetDate(string grouped)
		{
			if (grouped == "month")
				return string.Format("{0}.{1}", WriteOffDate.Month.ToString("00"), WriteOffDate.Year);
			if (grouped == "year")
				return string.Format("{0}", WriteOffDate.Year);
			return string.Format("{0}.{1}.{2}", WriteOffDate.Day.ToString("00"), WriteOffDate.Month.ToString("00"), WriteOffDate.Year);
		}
	}
}