using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "street", NameType = typeof(Street))]
	public class Street : BaseModel
	{
		public Street()
		{
		}

		public Street(string name)
		{
			Name = name;
		}

		[Property, NotEmpty(Message = "Введите номер улицы")]
		public virtual string Name { get; set; }

		[Property]
		public virtual string District { get; set; }

		[ManyToOne(Column = "Region")]
		public virtual Region Region { get; set; }
	}
}