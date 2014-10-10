using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	public class BaseModel
	{
		[Id(Name = "Id")]
		public virtual int Id { get; set; }
	}
}