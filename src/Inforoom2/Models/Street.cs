using System.Collections.Generic;
using System.ComponentModel;
using Inforoom2.Intefaces;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель улицы
	/// </summary>
	[Class(0, Table = "street", NameType = typeof(Street)), Description("Улица")]
	public class Street : BaseModel
	{
		public Street()
		{
			Confirmed = false;
			Houses = new List<House>();
		}

		public Street(string name) : this()
		{
			Name = name;
		}

		public Street(string street, Region region)
			: this()
		{
			Name = street;
			Region = region;
		}

		[Property(Column = "Name"), NotEmpty(Message = "Введите номер улицы"), Description("Наименование улицы")]
		public virtual string Name { get; set; }
		
		[Property, Description("Псевдоним улицы")]
		public virtual string Alias { get; set; }

		[Property, NotNullNotEmpty(Message = "Геопозиция должна быть задана"), Description("Геометка улицы на карте")]
		public virtual string Geomark { get; set; }

		[ManyToOne(Column = "Region", Cascade = "save-update")]
		public virtual Region Region { get; set; }

		[Property, Description("Маркер, отражающий, подтвержден ли дом Яндексом или нет")]
		public virtual bool Confirmed { get; set; }

		[Bag(0, Table = "House", Cascade = "all-delete-orphan")]
		[Key(1, Column = "Street")]
		[OneToMany(2, ClassType = typeof(House))]
		public virtual IList<House> Houses { get; set; }

		public virtual string PublicName()
		{
			return string.IsNullOrEmpty(Alias) ? Name : Alias;
		}
	}
}