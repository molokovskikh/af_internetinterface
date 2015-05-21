using System.Collections.Generic;
using System.Linq;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель сотрудника (администратора)
	/// </summary>
	[Class(0, Table = "Partners", NameType = typeof(Employee))]
	public class Employee : BaseModel
	{
		public Employee()
		{
			Roles = new List<Role>();
			Permissions = new List<Permission>();
			PaymentEmployee = new List<PaymentSystem>();
		}

		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string Login { get; set; }

		[Property]
		public virtual int? Categorie { get; set; }

		[Property]
		public virtual bool IsDisabled { get; set; }

		[Bag(0, Table = "user_role", Lazy = CollectionLazy.False)]
		[Key(1, Column = "user", NotNull = false)]
		[ManyToMany(2, Column = "role", ClassType = typeof(Role))]
		public virtual IList<Role> Roles { get; set; }

		[Bag(0, Table = "user_role", Lazy = CollectionLazy.False)]
		[Key(1, Column = "user", NotNull = false)]
		[ManyToMany(2, Column = "permission", ClassType = typeof(Permission))]
		public virtual IList<Permission> Permissions { get; set; }

		[Bag(0, Table = "paymentsystems", Lazy = CollectionLazy.False)]
		[Key(1, Column = "Employee", NotNull = false)]
		[ManyToMany(2, Column = "Id", ClassType = typeof(PaymentSystem))]
		public virtual IList<PaymentSystem> PaymentEmployee { get; set; }

		public virtual bool IsPaymentSystem()
		{
			var ret = PaymentEmployee.Count > 0;
			return ret;
		}

		/// <summary>
		/// Проверяет, есть ли у клиента права на какой-либо контент или страницу.
		/// В данный момент только проверяются доступы к старницам на основе ролей.
		/// </summary>
		/// <param name="access">Название права</param>
		/// <returns></returns>
		public virtual bool HasAccess(string access)
		{
			var hasPermission = Roles.Any(i => i.Permissions.Any(j => j.Name.ToLower() == access.ToLower()));
			return hasPermission;
		}
	}
}