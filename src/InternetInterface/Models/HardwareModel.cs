using System.ComponentModel;
using Castle.ActiveRecord;
using Castle.Components.Validator;

namespace InternetInterface.Models
{
	[ActiveRecord("inforoom2_hardwaremodels", Schema = "Internet", Lazy = true)]
	public class HardwareModel
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[BelongsTo(NotNull = true), Description("Тип оборудования")]
		public virtual RentalHardware Hardware { get; set; }

		[Property(Column = "Model"), ValidateNonEmpty("Укажите название модели")]
		public virtual string Name { get; set; }

		[Property, ValidateNonEmpty("Укажите серийный номер")]
		public virtual string SerialNumber { get; set; }
	}
}
