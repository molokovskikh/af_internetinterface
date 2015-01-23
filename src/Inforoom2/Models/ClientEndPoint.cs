using System.Net;
using Inforoom2.Helpers;
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
	}
}