using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "house", NameType = typeof(House))]
	public class House : BaseModel
	{
		public House()
		{
		}

		public House(string number, string housing)
		{
			Number = number;
			Housing = housing;
		}

		[ManyToOne(Column = "Street",  Cascade = "save-update"), NotNull]
		public virtual Street Street { get; set; }

		[Property, NotEmpty(Message = "Введите номер дома"), IsNumeric]
		public virtual string Number { get; set; }

		[Property]
		public virtual string Housing { get; set; }
		
		[Property]
		public virtual int ApartmentAmount { get; set; }

		[Property]
		public virtual int EntranceAmount { get; set; }

		public virtual string FullName{get { return Number + Housing; }}
	}
}