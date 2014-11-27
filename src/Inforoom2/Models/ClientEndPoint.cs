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

		[Property]
		public virtual bool Monitoring { get; set; }

		[Property]
		public virtual int? ActualPackageId { get; set; }

		public virtual void UpdateActualPackageId(int? packageId)
		{
			ActualPackageId = packageId;
		}
	}
}