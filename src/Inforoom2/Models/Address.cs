using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "address", NameType = typeof(Address))]
	public class Address : BaseModel
	{
		[ManyToOne(Column = "house", Cascade = "save-update")]
		public virtual House House { get; set; }

		[Property, Min(Value = 1)]
		public virtual int Entrance { get; set; }

		[Property, Min(Value = 1)]
		public virtual int Apartment { get; set; }

		[Property, Min(Value = 1)]
		public virtual int Floor { get; set; }
	}
}