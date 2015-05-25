using System.ComponentModel;
using Castle.ActiveRecord;

namespace InternetInterface.Models
{
	[ActiveRecord("inforoom2_rentalhardware", Schema = "Internet", Lazy = true)]
	public class RentalHardware
	{
		[PrimaryKey]
		public virtual uint Id { get; set; }

		[Property(NotNull = true), Description("Название")]
		public virtual string Name { get; set; }

		[Property(NotNull = true), Description("Стоимость")]
		public virtual decimal Price { get; set; }

		[Property(NotNull = true), Description("Количество бесплатных дней")]
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
