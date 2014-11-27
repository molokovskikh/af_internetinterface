using System;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "LawyerPerson", Schema = "internet", NameType = typeof(LegalClient))]
	public class LegalClient : BaseModel
	{
		[Property(NotNull = true)]
		public virtual decimal Balance { get; set; }

		[Property(NotNull = true)]
		public virtual DateTime PeriodEnd { get; set; }
	}
}