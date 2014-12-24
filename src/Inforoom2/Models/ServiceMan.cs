using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using InternetInterface.Models;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;
using Inforoom2.Models;

namespace Inforoom2.Models
{
	[Class(0, Table = "ServiceMen", NameType = typeof(ServiceMan))]
	public class ServiceMan : BaseModel
	{
		public ServiceMan()
		{
			ServiceRequests = new List<ServiceRequest>();
			ClientRequests = new List<ClientRequest>();
		}

		public ServiceMan(Employee employee) : this()
		{
			Employee = employee;
		}

		[ManyToOne(Column = "Employee"),NotNull]
		public virtual Employee Employee { get; set; }

		[ManyToOne(Column = "Region"), NotNull]
		public virtual Region Region { get; set; }

		[ManyToOne(Column = "ServiceTeam")]
		public virtual ServiceTeam ServiceTeam { get; set; }

		[Bag(0, Table = "ServiceRequest")]
		[Key(1, Column = "Performer")]
		[OneToMany(2, ClassType = typeof(ServiceRequest))]
		public virtual IList<ServiceRequest> ServiceRequests { get; set; }

		[Bag(0, Table = "requests")]
		[Key(1, Column = "_ServiceMan")]
		[OneToMany(2, ClassType = typeof(ClientRequest))]
		public virtual IList<ClientRequest> ClientRequests { get; set; }
	}

	[Class(0, Table = "ConnectBrigads", NameType = typeof(ServiceTeam))]
	public class ServiceTeam : BaseModel
	{
		public ServiceTeam()
		{
			Disabled = false;
		}

		public ServiceTeam(Region region)
			: this()
		{
			Region = region;
		}

		[Property(Column = "Name"), NotNullNotEmpty]
		public virtual string Name { get; set; }

		[Property(Column = "IsDisabled"), NotNullNotEmpty]
		public virtual bool Disabled { get; set; }

		[ManyToOne(Column = "Region"),NotNull]
		public virtual Region Region { get; set; }
	}

	[Class(0, Table = "ServiceRequest", NameType = typeof(ServiceRequest))]
	public class ServiceRequest : BaseModel
	{
		public ServiceRequest()
		{
			
		}
		public ServiceRequest(Client client) : this()
		{
			Client = client;
		}

		[ManyToOne(Column = "Performer")]
		public virtual ServiceMan ServiceMan { get; set; }

		[ManyToOne(Column = "Client")]
		public virtual Client Client { get; set; }

		[Property(Column = "Description"),NotNullNotEmpty]
		public virtual string Description { get; set; }
	
		[Property(Column = "BlockForRepair")]
		public virtual bool BlockNetwork { get; set; }

		[Property(Column = "PerformanceDate")]
		public virtual DateTime BeginTime { get; set; }

		[Property(Column = "RegDate")]
		public virtual DateTime CreationDate { get; set; }

		[Property(Column = "_EndTime")]
		public virtual DateTime EndTime { get; set; }
	}
}