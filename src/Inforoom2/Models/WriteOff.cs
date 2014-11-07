using System;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "UserWriteOffs", NameType = typeof(UserWriteOff))]
	public class UserWriteOff : BaseModel
	{
		[Property, Min(1)]
		public virtual decimal Sum { get; set; }

		[Property]
		public virtual DateTime Date { get; set; }

		[ManyToOne(Column = "client")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual bool BillingAccount { get; set; }

		[Property, NotEmpty(Message = "Введите комментарий")]
		public virtual string Comment { get; set; }

		[ManyToOne(Column = "Registrator")]
		public virtual Employee Registrator { get; set; }
	}
}