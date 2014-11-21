﻿using System.ComponentModel.DataAnnotations;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "SwitchAddress", NameType = typeof (SwitchAddress))]
	public class SwitchAddress : BaseModel
	{
		[ManyToOne(Column = "House", Cascade = "save-update")]
		public virtual House House { get; set; }

		[ManyToOne(Column = "Switch", Cascade = "save-update")]
		public virtual Switch Switch { get; set; }

		[ManyToOne(Column = "Street")]
		public virtual Street Street { get; set; }

		[Property]
		public virtual string Start { get; set; }

		[Property]
		public virtual string End { get; set; }

		[Property]
		public virtual StreetSide Side { get; set; }

		[Property]
		public virtual int Entrance { get; set; }

		[Property]
		public virtual int Apartment { get; set; }

		[Property]
		public virtual int Floor { get; set; }

		public virtual bool IsPrivateSector
		{
			get { return Street != null; }
		}
	}

	public enum StreetSide
	{
		[Display(Name = "Четная")] Even = 1,
		[Display(Name = "Нечетная")] Odd = 2
	}
}