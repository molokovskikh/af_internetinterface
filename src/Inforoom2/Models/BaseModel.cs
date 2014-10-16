using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Базовая модель, от которой все наследуется
	/// </summary>
	public class BaseModel
	{
		[Id(0,Name = "Id")]
		[Generator(1,Class = "native")]
		public virtual int Id { get; set; }
	}
}