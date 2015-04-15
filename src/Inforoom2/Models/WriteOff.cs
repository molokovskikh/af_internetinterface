using System;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(Table = "WriteOff", NameType = typeof(WriteOff))]
	public class WriteOff : BaseModel
	{
		[Property]
		public virtual decimal WriteOffSum { get; set; }

		[Property]
		public virtual decimal VirtualSum { get; set; }

		[Property]
		public virtual decimal MoneySum { get; set; }

		[ManyToOne(Column = "client")]
		public virtual Client Client { get; set; }

		[Property]
		public virtual DateTime WriteOffDate { get; set; }

		[Property]
		public virtual decimal? Sale { get; set; }

		[Property]
		public virtual decimal? BeforeWriteOffBalance { get; set; }
	}
}