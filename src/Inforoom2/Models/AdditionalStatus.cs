using System.ComponentModel;
using NHibernate.Mapping.Attributes;

namespace Inforoom2.Models
{
	/// <summary>
	/// Дополнительный статус
	/// </summary>
	[Class(0, Table = "AdditionalStatus", NameType = typeof(AdditionalStatus))]
	public class AdditionalStatus : BaseModel
	{
		[Property, Description("Наименование дополнительного статуса")]
		public virtual string Name { get; set; }

		[Property, Description("Короткое обозначение дополнительного статуса")]
		public virtual string ShortName { get; set; }
	}
}