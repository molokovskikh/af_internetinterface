using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using InternetInterface.Helpers;

namespace InternetInterface.Models
{
	[ActiveRecord("Internetsessionslogs", Schema = "Logs", Lazy = true)]
	public class Internetsessionslog : ActiveRecordLinqBase<Internetsessionslog>
	{
		[PrimaryKey(Generator = PrimaryKeyType.Assigned)]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual ClientEndpoint EndpointId { get; set; }

		[Property]
		public virtual string IP { get; set; }

		[Property]
		public virtual string HwId { get; set; }

		[Property]
		public virtual DateTime LeaseBegin { get; set; }

		[Property]
		public virtual DateTime? LeaseEnd { get; set; }

		public virtual string GetNormalIp()
		{
			return IpHelper.GetNormalIp(IP);
		}
	}
}