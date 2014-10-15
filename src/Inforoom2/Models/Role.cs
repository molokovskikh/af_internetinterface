using System.Collections.Generic;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "roles", NameType = typeof(Role))]
	public class Role : BaseModel
	{
		[Property]
		public virtual string Name { get; set; }


		[Bag(0, Table = "perm_role", Lazy = CollectionLazy.False)]
		[Key(1, Column = "role",NotNull = false)]
		[ManyToMany(2, Column = "permission", ClassType = typeof(Permission))]
		public virtual IList<Permission> Permissions { get; set; }


		[Bag(0, Table = "user_role", Lazy = CollectionLazy.False)]
		[Key(1, Column = "role",NotNull = false)]
		[ManyToMany(2, Column = "user", ClassType = typeof(Employee))]
		public virtual IList<Employee> Users { get; set; }
	}
}