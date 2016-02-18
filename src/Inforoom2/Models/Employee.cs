using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Inforoom2.validators;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель сотрудника (администратора)
	/// </summary>
	[Class(0, Table = "Partners", NameType = typeof (Employee))]
	public class Employee : BaseModel
	{
		public Employee()
		{
			Roles = new List<Role>();
			Permissions = new List<Permission>();
			EmployeePayments = new List<PaymentsForEmployee>();
			RegistrationDate = DateTime.Now;
			 
		}

		public static TimeSpan DefaultWorkBegin => new TimeSpan(9, 0, 0);

		public static TimeSpan DefaultWorkEnd => new TimeSpan(19, 0, 0);

		public static TimeSpan DefaultWorkStep => new TimeSpan(0, 30, 0);

		[Property, Description("Имя сотрудника"), NotNullNotEmpty(Message = "Необходимо ввести имя сотрудника")]
		public virtual string Name { get; set; }

		[Property, Description("Имя сотрудника")]
		public virtual string Email { get; set; }

		[Property(Column = "TelNum")]
		public virtual string PhoneNumber { get; set; }

		[Property, Description("Логин сотрудника"), NotNullNotEmpty(Message = "Необходимо ввести логин сотрудника")]
		public virtual string Login { get; set; }

		[Property(Column = "Adress"), Description("Адрес")]
		public virtual string Address { get; set; }

		[Property(Column = "RegDate")]
		public virtual DateTime RegistrationDate { get; set; }

		[Property]
		public virtual int? Categorie { get; set; }

		[Property]
		public virtual bool IsDisabled { get; set; }

		[Property]
		public virtual TimeSpan? WorkBegin { get; set; }

		[Property]
		public virtual TimeSpan? WorkEnd { get; set; }

		[Property]
		public virtual TimeSpan? WorkStep { get; set; }

		[Property]
		public virtual bool ShowContractOfAgency { get; set; }

		[Bag(0, Table = "roletouser", Lazy = CollectionLazy.False)]
		[NHibernate.Mapping.Attributes.Key(1, Column = "user", NotNull = false)]
		[ManyToMany(2, Column = "role", ClassType = typeof (Role))]
		public virtual IList<Role> Roles { get; set; }

		[Bag(0, Table = "permissiontouser", Lazy = CollectionLazy.False)]
		[NHibernate.Mapping.Attributes.Key(1, Column = "user", NotNull = false)]
		[ManyToMany(2, Column = "permission", ClassType = typeof (Permission))]
		public virtual IList<Permission> Permissions { get; set; }

		[Bag(0, Table = "PaymentsForAgent", Cascade = "all-delete-orphan")]
		[NHibernate.Mapping.Attributes.Key(1, Column = "Agent")]
		[OneToMany(2, ClassType = typeof (PaymentsForEmployee))]
		public virtual IList<PaymentsForEmployee> EmployeePayments { get; set; }

		public virtual bool IsPaymentSystem()
		{
			var ret = EmployeePayments.Count > 0;
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
			var hasPermissionFromRoles = Roles.Any(i => i.Permissions.Any(j => j.Name.ToLower() == access.ToLower()));
			var hasPermissionAsIs = Permissions.Any(j => j.Name.ToLower() == access.ToLower());
			return hasPermissionFromRoles || hasPermissionAsIs;
		}
	}
}