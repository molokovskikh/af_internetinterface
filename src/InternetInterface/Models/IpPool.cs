using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	[ActiveRecord("ippools", Schema = "internet", Lazy = true)]
	public class IpPool : ChildActiveRecordLinqBase<IpPool>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		//будь бдителен адреса хранятся в bigendian формате
		[Property]
		public virtual uint Begin { get; set; }

		//будь бдителен адреса хранятся в bigendian формате
		[Property]
		public virtual uint End { get; set; }

		[Property]
		public virtual int LeaseTime { get; set; }

		[Property]
		public virtual bool IsGray { get; set; }
	}
}