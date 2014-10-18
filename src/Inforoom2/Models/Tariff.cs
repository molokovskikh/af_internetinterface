using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "Tariff", NameType = typeof(Tariff))]
	public class Tariff : BaseModel
	{
		[Property]
		public virtual string Name { get; set; }
	}
}