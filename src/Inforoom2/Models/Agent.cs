using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "agents", NameType = typeof(Agent))]
	public class Agent : BaseModel
	{
		[Property, NotEmpty(Message = "Введите имя агента")]
		public virtual string Name { get; set; }

		[Property]
		public virtual bool Active { get; set; }
	}
}