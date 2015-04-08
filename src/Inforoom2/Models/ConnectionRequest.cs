using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "ConnectionRequests", NameType = typeof(ConnectionRequest))]
	public class ConnectionRequest : BaseModel
	{
		[Property]
		public virtual string Comment { get; set; }

		[ManyToOne(Column = "client", NotNull = true)]
		public virtual Client Client { get; set; }

		[ManyToOne]
		public virtual ServiceMan ServiceMan { get; set; }

		[Property]
		public virtual DateTime? BeginTime { get; set; }

		[Property]
		public virtual DateTime? EndTime { get; set; }
	}
}