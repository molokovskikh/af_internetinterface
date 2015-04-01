using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "user_role", NameType = typeof(Administrator))]
	public class Administrator : BaseModel
	{
		public Administrator()
		{
			Role = 1;
		}

		public Administrator(Employee employee) : this()
		{
			Employee = employee;
		}

		[ManyToOne(Column = "User"), NotNull]
		public virtual Employee Employee { get; set; }

		[Property]
		public virtual int Role { get; set; }
	}
}