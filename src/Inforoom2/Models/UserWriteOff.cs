using System;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "UserWriteOffs", Schema = "internet", NameType = typeof(UserWriteOff))]
	public class UserWriteOff : BaseModel
	{
		public UserWriteOff(Client client, decimal sum, string comment)
		{
			Client = client;
			Sum = sum;
			Comment = comment;
		}

		public UserWriteOff()
		{
		}

		[ManyToOne(Column = "Client")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[Property]
		public virtual decimal Sum { get; set; }

		[Property, NotEmpty]
		public virtual string Comment { get; set; }
	}
}