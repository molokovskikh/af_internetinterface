using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель прав
	/// </summary>
	[Class(0, Table = "permissions", NameType = typeof(Permission))]
	public class Permission : BaseModel
	{
		[Property, Description("Наименование права")]
		public virtual string Name { get; set; }

		[Property, Description("Описание права")]
		public virtual string Description { get; set; }

		[Bag(0, Table = "perm_role", Lazy = CollectionLazy.False)]
		[Key(1, Column = "permission", NotNull = false)]
		[ManyToMany(2, Column = "role", ClassType = typeof(Role))]
		public virtual IList<Role> Roles { get; set; }

		[Bag(0, Table = "permissiontouser", Lazy = CollectionLazy.False)]
		[Key(1, Column = "permission", NotNull = false)]
		[ManyToMany(2, Column = "user", ClassType = typeof(Employee))]
		public virtual IList<Employee> Users { get; set; }
	}
}