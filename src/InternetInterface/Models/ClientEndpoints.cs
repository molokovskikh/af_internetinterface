using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("ClientEndpoints", Schema = "Internet", Lazy = true)]
	public class ClientEndpoints : ActiveRecordLinqBase<ClientEndpoints>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual bool Disabled { get; set; }

		[BelongsTo("Client")]
		public virtual Clients Client { get; set; }

		[Property]
		public virtual int VLan { get; set; }

		[Property]
		public virtual int Module { get; set; }

		[Property]
		public virtual int Port { get; set; }

		[BelongsTo("Switch")]
		public virtual NetworkSwitches Switch { get; set; }

		[Property]
		public virtual int Monitoring { get; set; }

		[Property]
		public virtual int PackageId { get; set; }
	}
}