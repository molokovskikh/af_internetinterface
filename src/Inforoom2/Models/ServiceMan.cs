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
			ConnectionRequests = new List<ConnectionRequest>();
		}

		public ServiceMan(Employee employee) : this()
		{
			Employee = employee;
		}

		[ManyToOne(Column = "Employee"),NotNull]
		public virtual Employee Employee { get; set; }

		[OneToMany(1, ClassType = typeof(ServiceRequest))]
		public virtual IList<ServiceRequest> ServiceRequests { get; set; }


		[OneToMany(0, ClassType = typeof(ConnectionRequest))]
		public virtual IList<ConnectionRequest> ConnectionRequests { get; set; }
	}

	[Class(0, Table = "ServiceRequests", NameType = typeof(ServiceRequest))]
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

		[Property(Column = "Description")]
		public virtual string Description { get; set; }
	
		[Property(Column = "BlockForRepair")]
		public virtual bool BlockNetwork { get; set; }

		[Property(Column = "PerformanceDate")]
		public virtual DateTime BeginTime { get; set; }

		[Property(Column = "RegDate")]
		public virtual DateTime EndTime { get; set; }
	}

	[Class(0, Table = "ConnectGraph", NameType = typeof(ConnectionRequest))]
	public class ConnectionRequest : BaseModel
	{
		public ConnectionRequest()
		{
			
		}
		public ConnectionRequest(Client client) : this()
		{
			Client = client;
		}

		[ManyToOne(Column = "Brigad")]
		public virtual ServiceMan ServiceMan { get; set; }

		[ManyToOne(Column = "Client")]
		public virtual Client Client { get; set; }

		[Property(Column = "DateAndTime")]
		public virtual DateTime BeginTime { get; set; }

		[Property(Column = "DateAndTime")]
		public virtual DateTime EndTime { get; set; }
	}
}