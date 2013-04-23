using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Framework;
using InternetInterface.Helpers;
using NHibernate;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

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

		[Property("`ip`", ColumnType = "InternetInterface.Models.IPUserType, InternetInterface")]
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
			return IsGray((uint)Ip.Address);
		}

		public static bool IsGray(string ip)
		{
			if (string.IsNullOrEmpty(ip))
				return true;
			var programIp = NetworkSwitch.SetProgramIp(ip);
			if (string.IsNullOrEmpty(programIp))
				return true;
			return IsGray(uint.Parse(programIp));
		}

		public static bool IsGray(uint ip)
		{
			var grayPools = IpPool.Queryable.Where(i => i.IsGray).ToList();
			return grayPools.Any(grayPool => grayPool.Begin <= ip && grayPool.End >= ip);
		}

		public bool CanSelfRegister()
		{
			return Endpoint == null && Switch != null && Switch.Zone.IsSelfRegistrationEnabled;
		}
	}

	public class IPUserType : IUserType
	{
		public bool Equals(object x, object y)
		{
			return Object.Equals(x, y);
		}

		public int GetHashCode(object x)
		{
			return x.GetHashCode();
		}

		public object NullSafeGet(IDataReader rs, string[] names, object owner)
		{
			var obj = NHibernateUtil.UInt32.NullSafeGet(rs, names[0]);

			if (obj == null)
				return null;

			return new IPAddress(BigEndianConverter.GetBytes((int)obj));
		}

		public void NullSafeSet(IDbCommand cmd, object value, int index)
		{
			if (value == null) {
				((IDataParameter)cmd.Parameters[index]).Value = DBNull.Value;
			}
			else {
				var ip = (IPAddress)value;
				((IDataParameter)cmd.Parameters[index]).Value = BigEndianConverter.ToInt32(ip.GetAddressBytes());
			}
		}

		public object DeepCopy(object value)
		{
			return value;
		}

		public object Replace(object original, object target, object owner)
		{
			return original;
		}

		public object Assemble(object cached, object owner)
		{
			return cached;
		}

		public object Disassemble(object value)
		{
			return value;
		}

		public SqlType[] SqlTypes
		{
			get { return new[] { NHibernateUtil.UInt32.SqlType }; }
		}

		public Type ReturnedType
		{
			get { return typeof(IPAddress); }
		}

		public bool IsMutable
		{
			get { return false; }
		}
	}
}