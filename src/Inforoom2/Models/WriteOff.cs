using System;
using System.ComponentModel;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Денежные списания
	/// </summary>
	[Class(Table = "WriteOff", NameType = typeof(WriteOff))]
	public class WriteOff : BaseModel
	{
		[Property, Description("Общая сумма денежного списания")]
		public virtual decimal WriteOffSum { get; set; }

		[Property]
		public virtual decimal VirtualSum { get; set; }

		[Property, Description("Сумма списания с вычетом виртуальной суммы")]
		public virtual decimal MoneySum { get; set; }

		[ManyToOne(Column = "client")]
		public virtual Client Client { get; set; }

		[Property, Description("Дата денежного списания")]
		public virtual DateTime WriteOffDate { get; set; }

		[Property]
		public virtual decimal? Sale { get; set; }

		[Property]
		public virtual string Comment { get; set; }

		[Property, Description("Баланс клиента после денежного списания")]
		public virtual decimal? BeforeWriteOffBalance { get; set; }
	}
}