using System.Collections;
using System.Collections.Generic;
using NHibernate.Mapping.Attributes;


namespace Inforoom2.Models
{
	[Class(0, Table = "employee", NameType = typeof (Employee))]
	public class Employee : BaseModel
	{
		[Property]
		public virtual string Username { get; set; }

		[Property]
		public virtual string Password { get; set; }

		[Property]
		public virtual string Salt { get; set; }


		[Bag(0, Table = "user_role", Lazy = CollectionLazy.False)]
		[Key(1, Column = "user", NotNull = false)]
		[ManyToMany(2, Column = "role", ClassType = typeof(Role))]
		public virtual  IList<Role> Roles { get; set; }

		[Bag(0, Table = "user_role", Lazy = CollectionLazy.False)]
		[Key(1, Column = "user", NotNull = false)]
		[ManyToMany(2, Column = "permission", ClassType = typeof(Permission))]
		public virtual  IList<Permission> Permissions { get; set; }

	}

	
}