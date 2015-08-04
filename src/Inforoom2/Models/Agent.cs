using System.ComponentModel;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель агента
	/// </summary>
	[Class(0, Table = "agents", NameType = typeof(Agent))]
	public class Agent : BaseModel
	{
		[Property, NotEmpty(Message = "Введите имя агента"), Description("Имя агента")]
		public virtual string Name { get; set; }

		[Property, Description("Маркер, отражающий, активирован ли агент или нет")]
		public virtual bool Active { get; set; }
	}
}