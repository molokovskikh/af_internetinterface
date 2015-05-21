using System;
using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	/// <summary>
	/// Пользователь в redmine
	/// </summary>
	[ActiveRecord("users", Schema = "Redmine", Lazy = true)]
	public class RedmineUser : ChildActiveRecordLinqBase<RedmineUser>
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property]
		public virtual string Login { get; set; }

		[Property(Column = "firstname")]
		public virtual string FirstName { get; set; }

		[Property(Column = "lastname")]
		public virtual string LastName { get; set; }

		[Property(Column = "mail")]
		public virtual string Email { get; set; }

		[Property(Column = "admin")]
		public virtual bool IsAdmin { get; set; }

		[Property(Column = "last_login_on")]
		public virtual DateTime? LastLoginDate { get; set; }
	}
}
