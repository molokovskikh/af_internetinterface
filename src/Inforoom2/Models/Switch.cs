using System.Collections.Generic;
using System.Linq;
using System.Net;
using Inforoom2.Helpers;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "NetworkSwitches", NameType = typeof(Switch))]
	public class Switch : BaseModel
	{
		[Property]
		public virtual string Name { get; set; }

		[Property(Column = "Comment")]
		public virtual string Description { get; set; }

		[Property]
		public virtual string Mac { get; set; }
		
		[Property(Column = "TotalPorts")]
		public virtual int PortCount { get; set; }

		[Property(Column = "Ip", TypeType = typeof(IPUserType))]
		public virtual IPAddress Ip { get; set; }

		[Bag(0, Table = "clientendpoints", Cascade = "all-delete-orphan")]
		[Key(1, Column = "Switch")]
		[OneToMany(2, ClassType = typeof(ClientEndpoint))]
		public virtual IList<ClientEndpoint> Endpoints { get; set; }

		[ManyToOne]
		public virtual NetworkNode NetworkNode { get; set; }
	}
}