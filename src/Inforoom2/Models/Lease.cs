using System;
using System.Linq;
using System.Net;
using Inforoom2.Components;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Выданные Ip-адреса пользователям (динамические)
	/// </summary>
	[Class(0, Table = "Leases", NameType = typeof(Lease))]
	public class Lease : BaseModel
	{
		[ManyToOne(Column = "Endpoint", Cascade = "save-update")]
		public virtual ClientEndpoint Endpoint { get; set; }

		[Property(Column = "Ip", TypeType = typeof(IPUserType))]
		public virtual IPAddress Ip { get; set; }

		[ManyToOne(Column = "Switch")]
		public virtual Switch Switch { get; set; }

		[Property]
		public virtual DateTime LeaseBegin { get; set; }

		[Property]
		public virtual DateTime LeaseEnd { get; set; }

		[Property]
		public virtual int Port { get; set; }

		[ManyToOne(Column = "Pool", Cascade = "save-update")]
		public virtual IpPool Pool { get; set; }

		public static Lease GetLeaseForIp(string ipstr, ISession session)
		{
			try {
				var address = IPAddress.Parse(ipstr);
				var lease = session.Query<Lease>().FirstOrDefault(l => l.Ip == address);
				return lease;
			}
			catch (Exception e) {
				EmailSender.SendDebugInfo("Не удалось распарсить ip: " + ipstr, e.ToString());
				return null;
			}
		}
	}
}