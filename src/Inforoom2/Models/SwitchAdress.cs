using System.ComponentModel.DataAnnotations;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "SwitchAddress", NameType = typeof(SwitchAddress))]
	public class SwitchAddress : BaseModel
	{
		[ManyToOne(Column = "House", Cascade = "save-update")]
		public virtual House House { get; set; }

		[ManyToOne(Column = "NetworkNode", Cascade = "save-update")]
		public virtual NetworkNode NetworkNode { get; set; }

		[ManyToOne(Column = "Street", Cascade = "save-update")]
		public virtual Street Street { get; set; }

		[Property]
		public virtual string Start { get; set; }

		[Property]
		public virtual string End { get; set; }

		[Property]
		public virtual StreetSide Side { get; set; }

		[Property]
		public virtual int? Entrance { get; set; }

		[Property]
		public virtual int Apartment { get; set; }

		[Property]
		public virtual int Floor { get; set; }

		//true если яндекс api нашел адрес
		[Property]
		public virtual bool IsCorrectAddress { get; set; }

		public virtual bool IsPrivateSector
		{
			get { return Street != null; }
		}

		public virtual string GetFullAddress(bool showEntrance = false)
		{
			var str = House.Street.Region.Name + ", " + House.Street.Name + ", " + House.Number;
			if (showEntrance && Entrance != 0)
				str += ", " + Entrance;
			return str;
		}
	}

	public enum StreetSide
	{
		[Display(Name = "Четная")] Even = 1,
		[Display(Name = "Нечетная")] Odd = 2
	}
}