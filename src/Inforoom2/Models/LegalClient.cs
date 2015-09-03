using System;
using System.ComponentModel;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Юридическое лицо
	/// </summary>
	[Class(0, Table = "LawyerPerson", Schema = "internet", NameType = typeof(LegalClient))]
	public class LegalClient : BaseModel
	{
		[Property(NotNull = true), Description("Баланс юридического лица")]
		public virtual decimal Balance { get; set; }

		[Property(NotNull = true)]
		public virtual DateTime PeriodEnd { get; set; }

		[ManyToOne(Column = "RegionId", Cascade = "save-update")]
		public virtual Region Region { get; set; }
	}
}