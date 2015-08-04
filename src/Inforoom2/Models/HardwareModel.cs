using System.ComponentModel;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	/// <summary>
	/// Модель оборудования
	/// </summary>
	[Class(0, Table = "HardwareModels", NameType = typeof(HardwareModel))]
	public class HardwareModel: BaseModel
	{
		[ManyToOne(NotNull = true), Description("Тип оборудования")]
		public virtual RentalHardware Hardware { get; set; }

		[Property(Column = "Model"), NotNullNotEmpty(Message = "Укажите название модели")]
		public virtual string Name { get; set; }

		[Property, NotNullNotEmpty(Message = "Укажите серийный номер")]
		public virtual string SerialNumber { get; set; }
	}
}
