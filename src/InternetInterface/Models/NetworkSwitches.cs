using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;

namespace InternetInterface.Models
{
	[ActiveRecord("NetworkSwitches", Schema = "Internet", Lazy = true)]
	public class NetworkSwitches : ChildActiveRecordLinqBase<NetworkSwitches>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Mac { get; set; }

		[Property]
		public virtual string Name { get; set; }
	}
}