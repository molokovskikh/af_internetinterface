using System;
using System.Linq;
using System.Net;
using Common.Tools;
using Inforoom2.Components;
using Inforoom2.Helpers;
using NHibernate;
using NHibernate.Linq;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "ClientEndpoints", Schema = "internet", NameType = typeof (ClientEndpoint))]
	public class ClientEndpoint : BaseModel
	{
		[Property]
		public virtual bool Disabled { get; set; }
		[Property]
		public virtual int? PackageId { get; set; }

		[ManyToOne(Column = "Client", Cascade = "save-update")]
		public virtual Client Client { get; set; }

		[ManyToOne(Column = "Switch")]
		public virtual Switch Switch { get; set; }

		[Property]
		public virtual int Port { get; set; }

		[Property]
		public virtual bool Monitoring { get; set; }

		[Property]
		public virtual int? ActualPackageId { get; set; }

		[Property(Column = "Ip",TypeType = typeof(IPUserType))]
		public virtual IPAddress Ip { get; set; }

		public virtual void UpdateActualPackageId(int? packageId)
		{
			ActualPackageId = packageId;
		}

		public static ClientEndpoint GetEndpointForIp(string ipstr, ISession session)
		{
			var lease = Lease.GetLeaseForIp(ipstr, session);
			if (lease != null && lease.Endpoint != null)
				return lease.Endpoint;

			var ips = session.Query<StaticIp>().ToList();
			ClientEndpoint endpoint = null;
			try {
				var address = IPAddress.Parse(ipstr);
				endpoint = ips.Where(ip => {
					if (ip.Ip == address.ToString())
						return true;
					if (ip.Mask != null) {
						var subnet = SubnetMask.CreateByNetBitLength(ip.Mask.Value);
						if (address.IsInSameSubnet(IPAddress.Parse(ip.Ip), subnet))
							return true;
					}
					return false;
				}).Select(s => s.EndPoint).FirstOrDefault();
			}
			catch (Exception e) {
				EmailSender.SendDebugInfo("Не удалось распарсить ip: "+ipstr,e.ToString());
				endpoint = null;
			}

			return endpoint;
		}
	}
}