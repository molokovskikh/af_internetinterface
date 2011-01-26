using System;
using Castle.ActiveRecord;
using Castle.ActiveRecord.Linq;

namespace InternetInterface.Models
{
	/*[ActiveRecord("RequestsConnection", Schema = "Internet", Lazy = true)]
	public class RequestsConnection : ActiveRecordLinqBase<RequestsConnection>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo("BrigadNumber")]
		public virtual Brigad BrigadNumber { get; set; }

		[BelongsTo("ManagerID")]
		public virtual Partner ManagerID { get; set; }

		[BelongsTo("ClientID")]
		public virtual PhisicalClients ClientID { get; set; }

		[Property]
		public virtual DateTime RegDate { get; set; }

		[Property]
		public virtual DateTime CloseDemandDate { get; set; }
	}*/

}