using Common.Tools;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Постоянный Ip-адрес клиента
	/// </summary>
	[Class(0, Table = "StaticIps", Schema = "internet", NameType = typeof (StaticIp))]
	public class StaticIp : BaseModel
	{
		public StaticIp()
		{
		}

		public StaticIp(ClientEndpoint endPoint, string ip)
		{
			EndPoint = endPoint;
			Ip = ip;
		}

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

		public virtual string GetSubnet()
		{
			if (Mask != null)
				return SubnetMask.CreateByNetBitLength(Mask.Value).ToString();
			return string.Empty;
		}
	}
}