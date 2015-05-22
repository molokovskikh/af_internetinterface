using System.ComponentModel;
using NHibernate.Mapping.Attributes;
using NHibernate.Validator.Constraints;

namespace Inforoom2.Models
{
	[Class(0, Table = "RentalHardware", NameType = typeof(RentalHardware))]
	public class RentalHardware: BaseModel
	{
		[Property(NotNull = true), Description("Название"), NotNullNotEmpty(Message = "Поле не может быть пустым")]
		public virtual string Name { get; set; }

		[Property(NotNull = true), Description("Стоимость"), DecimalMin(0.0, Message = "Введите число >= 0")]
		public virtual decimal Price { get; set; }

		[Property(NotNull = true), Description("Количество бесплатных дней"), Digits(3, 0, Message = "Введите число от 0 до 999")]
		public virtual uint FreeDays { get; set; }

		public virtual HardwareType Type
		{
			get
			{
				switch (Name) {
					case "ТВ-приставка":
						return HardwareType.TvBox;
					case "Коммутатор":
						return HardwareType.Switch;
					case "Роутер":
						return HardwareType.Router;
					case "ONU":
						return HardwareType.ONU;
					case "Конвертер":
						return HardwareType.Converter;
				}
				return HardwareType.None;
			}
		}
	}

	public enum HardwareType
	{
		[Description("Не существует")] None = 0,
		[Description("ТВ-приставка")] TvBox = 1,
		[Description("Коммутатор")] Switch,
		[Description("Роутер")] Router,
		[Description("ONU")] ONU,
		[Description("Конвертер")] Converter,
		[Description("Общее кол-во")] Count
	}
}
