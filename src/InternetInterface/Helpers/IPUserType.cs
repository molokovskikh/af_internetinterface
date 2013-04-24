using System;
using System.Data;
using System.Net;
using NHibernate;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;

namespace InternetInterface.Helpers
{
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

			return new IPAddress(BigEndianConverter.GetBytes((uint)obj));
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