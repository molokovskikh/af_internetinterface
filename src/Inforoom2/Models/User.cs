using System;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "Users", NameType = typeof (User))]
	public class User : BaseModel
	{
	
		[Property]
		public virtual string Username { get; set; }

		[Property]
		public virtual string Password { get; set; }

		[Property]
		public virtual string Roles { get; set; }

		[Property]
		public virtual string City { get; set; }
	}
}