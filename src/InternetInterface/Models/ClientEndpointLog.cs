using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	/// <summary>
	/// Модель используется для поиска удаленных endpoint клиента
	/// </summary>
	[ActiveRecord("clientendpointinternetlogs", Schema = "logs")]
	public class ClientEndpointLog : ChildActiveRecordLinqBase<ClientEndpointLog>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual int? Client { get; set; }

		[Property]
		public virtual int? ClientendpointId { get; set; }
	}
}