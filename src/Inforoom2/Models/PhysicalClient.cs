using NHibernate.Mapping;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	public class PhysicalClient : BaseModel
	{
		
		[ManyToOne(Column = "Address")]
		public virtual Address Address { get; set; } 
	}
}