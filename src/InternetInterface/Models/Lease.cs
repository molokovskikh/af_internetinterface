using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using InternetInterface.Helpers;

namespace InternetInterface.Models
{
	[ActiveRecord(Schema = "Internet", Table = "Leases")]
	public class Lease : ActiveRecordLinqBase<Lease>
	{
		private const string IPRegExp =
	@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b";

		[PrimaryKey]
		public uint Id { get; set; }

		[Property("`ip`")]
		public uint Ip { get; set; }

		[BelongsTo(Cascade = CascadeEnum.SaveUpdate)]
		public ClientEndpoints Endpoint { get; set; }

		[Property]
		public int Port { get; set; }

		[BelongsTo]
		public NetworkSwitches Switch { get; set; }

		[Property]
		public DateTime LeaseBegin { get; set; }

		[Property]
		public DateTime LeaseEnd { get; set; }

		[Property]
		public string LeasedTo { get; set; }

		[Property]
		public virtual uint? Pool { get; set; }

		[Property]
		public virtual int Module { get; set; }

		public string GetIp()
		{
			return IpHelper.GetNormalIp(Ip.ToString());
		}

		public string GetMac()
		{
			return LeasedTo.Substring(0, 17);
		}

		public bool CompareIp(string ip)
		{
			return GetIp().Equals(ip);
		}

		public bool CompareMac(string mac)
		{
			return GetMac().Equals(mac);
		}

		public virtual bool IsGray()
		{
			return IsGray(Ip);
		}

		public static bool IsGray(string ip)
		{
			if (string.IsNullOrEmpty(ip))
				return true;
			var programIp = NetworkSwitches.SetProgramIp(ip);
			if (string.IsNullOrEmpty(programIp))
				return true;
			return IsGray(uint.Parse(programIp));
		}

		public static bool IsGray(uint ip)
		{
			var grayPools = IpPool.Queryable.Where( i => i.IsGray ).ToList();
			return grayPools.Any(grayPool => grayPool.Begin <= ip && grayPool.End >= ip);
		}
	}
}