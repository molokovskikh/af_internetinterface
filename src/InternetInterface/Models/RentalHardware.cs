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
	}
}