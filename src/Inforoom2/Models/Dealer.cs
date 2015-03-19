using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "dealers", NameType = typeof(Dealer))]
	public class Dealer : BaseModel
	{
		[ManyToOne(Column = "Employee"), NotNull]
		public virtual Employee Employee { get; set; }
	}
}