﻿using System.ComponentModel;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "house", NameType = typeof(House)), Description("Дом")]
	public class House : BaseModel
	{
		public House()
		{
		}

		public House(string number)
		{
			Number = number;
		}

		public House(string number, Street street)
		{
			Number = number;
			Street = street;
		}

		[ManyToOne(Column = "Street", Cascade = "save-update"), NotNull, Description("Улица")]
		public virtual Street Street { get; set; }

		[Property, NotEmpty(Message = "Введите номер дома"), Description("Номер дома")]
		public virtual string Number { get; set; }

		[Property]
		public virtual int ApartmentAmount { get; set; }

		[Property]
		public virtual bool Confirmed { get; set; }

		[Property]
		public virtual string Geomark { get; set; }

		[Property]
		public virtual int EntranceAmount { get; set; }

		[ManyToOne(Column = "Region", Cascade = "save-update")]
		public virtual Region Region { get; set; }
	}
}