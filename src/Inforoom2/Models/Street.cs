using System.Collections.Generic;
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
			Confirmed = false;
			Houses = new List<House>();
		}

		public Street(string name):this()
		{
			Name = name;
		}

		public Street(string street, Region region)
			: this()
		{
			Name = street;
			Region = region;
		}

		[Property, NotEmpty(Message = "Введите номер улицы")]
		public virtual string Name { get; set; }

		[Property,NotNullNotEmpty(Message = "Геопозиция должна быть задана")]
		public virtual string Geomark { get; set; }

		[ManyToOne(Column = "Region", Cascade = "save-update")]
		public virtual Region Region { get; set; }

		[Property]
		public virtual bool Confirmed { get; set; }

		[Bag(0, Table = "House", Cascade = "all-delete-orphan")]
		[Key(1, Column = "Street")]
		[OneToMany(2, ClassType = typeof(House))]
		public virtual IList<House> Houses { get; set; }
	}
}