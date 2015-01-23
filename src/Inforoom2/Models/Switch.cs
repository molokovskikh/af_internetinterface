using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	[Class(0, Table = "NetworkSwitches", NameType = typeof(Switch))]
	public class Switch : BaseModel
	{
		[Property]
		public virtual string Name { get; set; }
	}
}