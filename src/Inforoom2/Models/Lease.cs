using System.Net;
using Inforoom2.Helpers;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "Leases", NameType = typeof(Lease))]
	public class Lease : BaseModel
	{
		[ManyToOne(Column = "Endpoint", Cascade = "save-update")]
		public virtual ClientEndpoint Endpoint { get; set; }

		[Property(Column = "Ip",TypeType = typeof(IPUserType))]
		public virtual IPAddress Ip { get; set; }

		[ManyToOne(Column = "Switch")]
		public virtual Switch Switch { get; set; }

		[Property]
		public virtual int Port { get; set; }
	}
}