using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "Internet", Table = "Leases")]
	public class Lease : ActiveRecordLinqBase<Lease>
	{
		public Lease()
		{
		}

		public Lease(ClientEndpoint endpoint)
		{
			Endpoint = endpoint;
		}

		[PrimaryKey]
		public uint Id { get; set; }

		[Property("`ip`", ColumnType = "InternetInterface.Helpers.IPUserType, InternetInterface")]
		public IPAddress Ip { get; set; }

		[BelongsTo(Cascade = CascadeEnum.SaveUpdate)]
		public ClientEndpoint Endpoint { get; set; }

		[Property]
		public int Port { get; set; }

		[BelongsTo]
		public NetworkSwitch Switch { get; set; }

		[Property]
		public DateTime LeaseBegin { get; set; }

		[Property]
		public DateTime LeaseEnd { get; set; }

		[Property]
		public string LeasedTo { get; set; }

		[BelongsTo]
		public virtual IpPool Pool { get; set; }

		public string GetMac()
		{
			return LeasedTo.Substring(0, 17);
		}

		public bool CompareIp(string ip)
		{
			return ip == Ip.ToString();
		}

		public bool CompareMac(string mac)
		{
			return GetMac().Equals(mac);
		}

		public bool CanSelfRegister()
		{
			return Endpoint == null && Switch != null && Switch.Zone.IsSelfRegistrationEnabled;
		}
	}
}