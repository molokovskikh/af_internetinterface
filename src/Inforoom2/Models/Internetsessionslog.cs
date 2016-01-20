using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Web;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "Internetsessionslogs", Schema = "Logs", NameType = typeof(Internetsessionslog))]
	public class Internetsessionslog : BaseModel
	{
		[Property]
		public virtual int? EndpointId { get; set; }

		[Property]
		public virtual string IP { get; set; }

		[Property]
		public virtual string HwId { get; set; }

		[Property]
		public virtual DateTime LeaseBegin { get; set; }

		[Property]
		public virtual DateTime? LeaseEnd { get; set; }

		public virtual IPAddress GetIpString()
		{
			IPAddress ip = null;
			IPAddress.TryParse(IP, out ip);
			return ip;
		}

	}

	/// <summary>
	/// Модель используется для поиска удаленных endpoint клиента
	/// </summary>
	[Class(0, Table = "clientendpointinternetlogs", Schema = "Logs", NameType = typeof(ClientEndpointLog))]
	public class ClientEndpointLog : BaseModel
	{
		[Property]
		public virtual int? Client { get; set; }

		[Property]
		public virtual int? ClientendpointId { get; set; }
	}
}