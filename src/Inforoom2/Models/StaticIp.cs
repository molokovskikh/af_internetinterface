using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Постоянный Ip-адрес клиента
	/// </summary>
	[Class(0, Table = "StaticIps", Schema = "internet", NameType = typeof(StaticIp))]
	public class StaticIp : BaseModel
	{
		[ManyToOne(Column = "EndPoint", Cascade = "save-update")]
		public virtual ClientEndpoint EndPoint { get; set; }

		[Property]
		public virtual string Ip { get; set; }

		[Property]
		public virtual int? Mask { get; set; }

		public virtual string Address()
		{
			return Mask != null ? Ip + "/" + Mask : Ip;
		}
	}
}