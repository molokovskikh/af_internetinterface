﻿using Inforoom2.validators;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "address", NameType = typeof(Address))]
	public class Address : BaseModel
	{ 
		[ManyToOne(Column = "house", Cascade = "save-update"),ValidatorNotEmpty]
		public virtual House House { get; set; }

		[Property]
		public virtual string Entrance { get; set; }

		[Property]
		public virtual string Apartment { get; set; }

		[Property]
		public virtual int Floor { get; set; }

		//true если яндекс api нашел адрес
		[Property]
		public virtual bool IsCorrectAddress { get; set; }

		public virtual string FullAddress
		{
			get
			{
				return House.Street.Region.City.Name + ". "
				       + House.Street.Name + ". "
				       + House.Number;
			}
		}

		public virtual string AddressAsString { get; set; }
	}
}