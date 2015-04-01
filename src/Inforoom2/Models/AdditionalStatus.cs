using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "AdditionalStatus", NameType = typeof(AdditionalStatus))]
	public class AdditionalStatus : BaseModel
	{
		[Property]
		public virtual string Name { get; set; }

		[Property]
		public virtual string ShortName { get; set; }
	}
}