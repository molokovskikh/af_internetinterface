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
	public class StaticIp
	{
		public StaticIp()
		{
		}

		public StaticIp(ClientEndpoint endPoint, string ip)
		{
			EndPoint = endPoint;
			Ip = ip;
		}

		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo]
		public virtual ClientEndpoint EndPoint { get; set; }

		[Property]
		public virtual string Ip { get; set; }

		[Property]
		public virtual int? Mask { get; set; }

		public virtual string GetSubnet()
		{
			if (Mask != null)
				return SubnetMask.CreateByNetBitLength(Mask.Value).ToString();
			return string.Empty;
		}

		public virtual string Address()
		{
			return Mask != null ? Ip + "/" + Mask : Ip;
		}
	}
}