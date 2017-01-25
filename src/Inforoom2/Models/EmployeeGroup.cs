using System.Collections.Generic;
using System.ComponentModel;
using NHibernate.Engine;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель роли
	/// </summary>
	[Class(0, Table = "EmployeeGroup", NameType = typeof(EmployeeGroup))]
	public class EmployeeGroup : BaseModel
	{
		public EmployeeGroup()
		{
			EmployeeList = new List<Employee>();
		}

		[Property, Description("Наименование группы")]
		public virtual string Name { get; set; }

		[Bag(0, Table = "employeeToGroup")]
		[Key(1, Column = "GroupId", NotNull = false)]
		[ManyToMany(2, Column = "EmployeeId", ClassType = typeof (Employee))]
		public virtual IList<Employee> EmployeeList { get; set; }
		
	}
}