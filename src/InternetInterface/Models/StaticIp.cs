using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using Common.Tools;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "Internet", Table = "StaticIps")]
	public class StaticIp : ActiveRecordLinqBase<StaticIp>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual ClientEndpoints EndPoint { get; set; }

		[Property]
		public virtual string Ip { get; set; } 
		
		 /*[Property]
		public virtual string Gateway { get; set; } */
		
		[Property]
		public virtual int? Mask { get; set; }

		public virtual string GetSubnet()
		{
			if (Mask != null)
				return SubnetMask.CreateByNetBitLength(Mask.Value).ToString();
			return string.Empty;
		}
	}
}