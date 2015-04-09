using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "address", NameType = typeof(Address))]
	public class Address : BaseModel
	{
		[ManyToOne(Column = "house", Cascade = "save-update")]
		public virtual House House { get; set; }

		[Property]
		public virtual int Entrance { get; set; }

		[Property, Min(Value = 1, Message = "Введите номер квартиры")]
		public virtual int Apartment { get; set; }

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